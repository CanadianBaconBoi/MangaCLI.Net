#region header

// MangaCLI.Net : A Featureful Manga Downloader
// Copyright (C)  2024 canadian
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using CommandLine;
using MangaCLI.Net.Connectors.Manga;
using MangaCLI.Net.Metadata;
using MangaCLI.Net.Models;
using MimeTypes;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SkiaSharp;
using MylarJsonContext = MangaCLI.Net.Metadata.MylarJsonContext;

namespace MangaCLI.Net;

internal static class MangaCli
{
    public static IConnector Connector = null!;

    public static readonly Config Config = Config.FromFile(Environment.OSVersion.Platform switch
    {
        PlatformID.Unix => ResolvePath(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config/mangacli.toml")),
        PlatformID.Win32NT => ResolvePath(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MangaCLI/mangacli.toml")),
        _ => throw new ArgumentOutOfRangeException(
            $"Operating system {Environment.OSVersion.VersionString} is not supported")
    }).Result;
    

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CommandLineOptions))]
    public static async Task Main(string[] args)
    {
        var options = await ValidateCommandLine(Parser.Default.ParseArguments<CommandLineOptions>(args));
        Connector = IConnector.Connectors[options.Source].Invoke();
        
        foreach (var comicName in options.SearchQuery)
        {
            var result = await DownloadComic(options.Clone(), comicName);
            var comicInfo = await result.Comic!.GetComicInfo();
            switch (result.Result)
            {
                case DownloadResult.NotFound:
                    await Console.Error.WriteLineAsync($"No comic found for \"{comicName}\"");
                    break;
                case DownloadResult.NoChapters:
                    await Console.Error.WriteLineAsync($"No chapters found for comic \"{result.Comic!.Title}\" ({comicInfo.Title})");
                    break;
                case DownloadResult.NoGoodChapters:
                    await Console.Error.WriteLineAsync($"No chapters found for comic \"{result.Comic!.Title}\" ({comicInfo.Title}) with Scanlation Group {options.ScanlationGroup}");
                    await Console.Error.WriteLineAsync($"Try a different Scanlation Group or enable Alternate Groups with --allow-alternate");
                    break;
            }
        }
    }

    private static async Task<CommandLineOptions> ValidateCommandLine(ParserResult<CommandLineOptions> optionsResult)
    {
        foreach (var error in optionsResult.Errors)
        {
            switch (error.Tag)
            {
                case ErrorType.UnknownOptionError:
                case ErrorType.RepeatedOptionError:
                case ErrorType.MissingRequiredOptionError:
                    await Console.Error.WriteLineAsync("Error parsing command line arguments");
                    break;
                default:
                    await Console.Error.WriteLineAsync("Unknown error parsing command line arguments");
                    break;
            }

            Environment.Exit(1);
            return null;
        }

        var options = optionsResult.Value;

        // The string sanitizer isn't great on CommandLineParser, but it's a small library so we tolerate it
        if (!IConnector.Connectors.ContainsKey(options.Source))
        {
            await Console.Error.WriteLineAsync($"Source \"{options.Source}\" does not exist");
            Environment.Exit(1);
            return null;
        }

        options.OutputFolder = ResolvePath(options.OutputFolder);

        if (!Directory.Exists(options.OutputFolder))
            Directory.CreateDirectory(options.OutputFolder);

        if (options.SearchQuery.Any()) return options;

        List<string> queries = [];
        await using var fs = File.OpenRead(ResolvePath(options.SearchQueryFile));
        using var streamReader = new StreamReader(fs, Encoding.UTF8, true, 128);
        while (await streamReader.ReadLineAsync() is { } line)
            queries.Add(line);
        options.SearchQuery = queries.AsEnumerable();

        return options;
    }

    private static async Task<(IComic? Comic, DownloadResult Result)> DownloadComic(CommandLineOptions options, string comicName)
    {
        var comic = await FindComic(comicName, options.SearchSelection);
        if (comic == null)
            return (null, DownloadResult.NotFound);

        var comicInfo = await comic.GetComicInfo();

        await Console.Out.WriteLineAsync($"Found Comic: \"{comic.Title}\" ({comicInfo.Title})");

        var chapters = comic.GetChapters(options.Language).GetAsyncEnumerator();
        if (!await chapters.MoveNextAsync())
            return (comic, DownloadResult.NoChapters);

        var filteredChapters= await FilterChapters(chapters, options.ScanlationGroup, !options.DisallowAlternateGroups);
        if (filteredChapters.Count == 0)
            return (comic, DownloadResult.NoGoodChapters);

        if (string.IsNullOrEmpty(comicInfo.Title))
            comicInfo.Title = comic.Title;
        
        if (!options.NoSubfolder)
        {
            options.OutputFolder = Path.Combine(options.OutputFolder, NormalizePath(comicInfo.Title!));
            Directory.CreateDirectory(options.OutputFolder);
        }


        var mylarSeriesPath = Path.Combine(options.OutputFolder, "series.json");
        if (options.Overwrite || !File.Exists(mylarSeriesPath))
            await using (var fs = new FileStream(mylarSeriesPath, FileMode.Create))
                await JsonSerializer.SerializeAsync(fs,
                    MetadataMylar.FromComicInfo(comicInfo,
                        () => filteredChapters.First().Value.Pages.First().Url),
                    MylarJsonContext.Default.MetadataMylar);


        if (comicInfo.Covers?.FirstOrDefault() is { } cover)
        {
            var response = await Connector.GetClient().GetAsync(cover.Item2.Location);
            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception("Incorrect Status Code");
            if (response.Content.Headers.ContentType?.MediaType == "image/webp")
            {
                var coverPath = Path.Combine(options.OutputFolder, "cover.png");
                if (options.Overwrite || !File.Exists(coverPath))
                {
                    using var image = SKImage.FromEncodedData(await response.Content.ReadAsStreamAsync()).Encode(SKEncodedImageFormat.Png, 100);
                    await using var fs = new FileStream(coverPath, FileMode.CreateNew);
                    await fs.WriteAsync(image.ToArray());
                }
            }
            else
            {
                var coverPath = Path.Combine(options.OutputFolder, $"cover{MimeTypeMap.GetExtension(response.Content.Headers.ContentType?.MediaType ?? "image/jpeg")}");
                if (options.Overwrite || !File.Exists(coverPath))
                    await using (var fs = new FileStream(coverPath, FileMode.Create))
                        await response.Content.CopyToAsync(fs);
            }
        }

        var fileIndex = 1;
        var failedChapterCount = 0;
        foreach (var (chapterIndex, chapter) in filteredChapters)
        {
            if (chapter.GroupName == null || chapter.GroupName.Length == 0)
                chapter.GroupName = ["UNKNOWN"];

            if (!await DownloadChapter(comicInfo, chapter, Path.Combine(
                    options.OutputFolder,
                    NormalizePath($"[{fileIndex++:0000}]_Chapter_{chapterIndex}_[{chapter.GroupName.First()}].{
                        options.Format switch {
                            OutputFormat.CBZ => "cbz",
                            OutputFormat.PDF => "pdf",
                            _ => throw new InvalidEnumArgumentException(nameof(options.Format), (int)options.Format, typeof(OutputFormat))
                        }
                    }")
                ), options.Format, options.Overwrite))
                failedChapterCount++;
        }

        await Console.Out.WriteLineAsync($"\nDownloaded Manga: \"{comic.Title}\" ({comicInfo.Title}) [{filteredChapters.Count - failedChapterCount}/{filteredChapters.Count} Chapters]");

        await Task.Delay(1000);
        return (comic, DownloadResult.Successful);
    }

    private static async Task<IComic?> FindComic(string searchQuery, SearchSelectionType searchType)
    {
        var comics = Connector.SearchComics(searchQuery);
        if (searchQuery == searchQuery.ToLower())
            searchQuery = string.Concat(searchQuery[0].ToString().ToUpper(), searchQuery.AsSpan(1));

        return searchType switch
        {
            SearchSelectionType.Exact => await comics.FirstOrDefaultAsync(c => c != null && c.Title.Equals(searchQuery)),
            SearchSelectionType.First => await comics.FirstOrDefaultAsync(),
            SearchSelectionType.Random => await comics.ToArrayAsync() is { Length: > 0 } comicArray
                ? comicArray[Random.Shared.Next(0, comicArray.Length)]
                : null,
            _ => null
        };
    }

    private static async Task<Dictionary<string, IChapter>> FilterChapters(IAsyncEnumerator<IChapter> chapters, string preferredGroup,
        bool useAlternateGroup)
    {
        var strCmp = StringComparer.InvariantCultureIgnoreCase;
        Dictionary<string, IChapter> chaptersToTake = new();
        do
        {
            var chapter = chapters.Current;
            if (chaptersToTake.TryGetValue(chapter.ChapterIndex ?? "0", out var chapterWeHave))
            {
                if ((chapterWeHave.GroupName == null || !chapterWeHave.GroupName.Contains(preferredGroup, strCmp))
                    && chapter.GroupName != null && chapter.GroupName.Contains(preferredGroup, strCmp))
                    (chapter, chapterWeHave) = (chapterWeHave, chaptersToTake[chapter.ChapterIndex ?? "0"] = chapter);

                if (string.IsNullOrEmpty(chapterWeHave.Title))
                    chapterWeHave.Title = chapter.Title;
                
                if (string.IsNullOrEmpty(chapterWeHave.VolumeIndex))
                    chapterWeHave.VolumeIndex = chapter.VolumeIndex;
            }
            else if (useAlternateGroup || chapter.GroupName?.Contains(preferredGroup, strCmp) is true)
                chaptersToTake[chapter.ChapterIndex ?? "0"] = chapter;
        } while (await chapters.MoveNextAsync());

        foreach (var chapter in chaptersToTake.Values.Where(chapter => string.IsNullOrEmpty(chapter.Title)))
            chapter.Title = $"Chapter {chapter.ChapterIndex ?? "0"}";

        return chaptersToTake;
    }
    
    private static async Task<bool> DownloadChapter(ComicInfo comicInfo, IChapter chapter, string outputFilePath, OutputFormat outputFormat, bool overwrite)
    {
        if (!overwrite && File.Exists(outputFilePath))
            return true;

        var comickRackMetadata = chapter.GetComicRackMetadata(comicInfo);

        await Console.Out.WriteAsync("\r" + new string(' ', Console.BufferWidth) + "\r");
        await Console.Out.WriteAsync($"Downloading Chapter {chapter.ChapterIndex} : {chapter.Title}");

        var tempDownloadDirectory = GetTempDirectory();

        var pages = chapter.Pages;
        var pageMap = new Dictionary<string, IPage>();

        using (var cts = new CancellationTokenSource())
        {
            try
            {
                await Parallel.ForAsync(0, pages.Length, new ParallelOptions
                    {
                        MaxDegreeOfParallelism = 2,
                        CancellationToken = cts.Token
                    },
                    async (i, token) =>
                    {
                    var page = pages[i];
                    var failCount = 0;
                    while (true)
                        try
                        {
                            using var response = await Connector.GetClient().GetAsync(page.Url, token);
                            if (response.StatusCode != HttpStatusCode.OK)
                                throw new Exception("Incorrect Status Code");
                            comickRackMetadata.Pages[i].ImageSize =
                                response.Content.Headers.ContentLength.GetValueOrDefault(0);
                            if (response.Content.Headers.ContentType?.MediaType == "image/webp")
                            {
                                var path = Path.Combine(tempDownloadDirectory, $"{i + 1:000000}.png");
                                using SKData image = SKImage.FromEncodedData(await response.Content.ReadAsStreamAsync(token)).Encode(SKEncodedImageFormat.Png, 100);
                                await using var fs = new FileStream(path, FileMode.Create);
                                fs.Write(image.AsSpan());
                            }
                            else
                            {
                                var path = Path.Combine(tempDownloadDirectory,
                                    $"{i + 1:000000}{MimeTypeMap.GetExtension(response.Content.Headers.ContentType?.MediaType ?? "image/jpeg")}");
                                pageMap[path] = page;
                                await using var fs = new FileStream(path, FileMode.CreateNew);
                                await response.Content.CopyToAsync(fs, token);
                            }

                            break;
                        }
                        catch
                        {
                            failCount++;
                            if (failCount == 1)
                                await Console.Out.WriteLineAsync();
                            if (failCount == 3)
                            {
                                await Console.Error.WriteLineAsync(
                                    $"Failed to request page {i + 1} three times, chapter {chapter.ChapterIndex} will be missing.");
                                // ReSharper disable once AccessToDisposedClosure
                                await cts.CancelAsync();
                                break;
                            }

                            var sleep = failCount switch
                            {
                                1 => 1,
                                2 => 10,
                                _ => throw new Exception("Impossibro")
                            };
                            await Console.Error.WriteLineAsync(
                                $"Failed to request page {i + 1}, retrying in {sleep} seconds.");
                            await Task.Delay(sleep * 1000, token);

                        }
                    });
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch { }

            if (cts.IsCancellationRequested)
                return false;
        }

        switch (outputFormat)
        {
            case OutputFormat.CBZ:
                await using (var mdFs = new FileStream(Path.Combine(tempDownloadDirectory, "ComicInfo.xml"), FileMode.CreateNew))
                    MetadataComicRack.Serialize(comickRackMetadata, mdFs);
                await using (var fs = new FileStream(outputFilePath, FileMode.Create))
                    ZipFile.CreateFromDirectory(tempDownloadDirectory, fs);
                break;
            case OutputFormat.PDF:
                var document = new PdfDocument();
                document.Language = comickRackMetadata.LanguageISO;
                document.Options.ColorMode = PdfColorMode.Rgb;
                document.Info.Title = comickRackMetadata.Title;
                document.Info.Author = comickRackMetadata.Writer;
                document.Info.Creator = "MangaCLI.Net";
                document.Info.Subject = comickRackMetadata.Series;

                foreach (var file in Directory.EnumerateFiles(tempDownloadDirectory))
                {
                    var page = document.AddPage();
                    page.Width = XUnit.FromPoint(pageMap[file].Width);
                    page.Height = XUnit.FromPoint(pageMap[file].Height);
                    page.Orientation = pageMap[file].Width > pageMap[file].Height
                        ? PageOrientation.Landscape
                        : PageOrientation.Portrait;

                    using var gfx = XGraphics.FromPdfPage(page);

                    XImage img;
                    try
                    {
                        img = XImage.FromFile(Path.Combine(tempDownloadDirectory, file));
                    }
                    catch
                    {
                        using var imageFile = SKImage.FromEncodedData(Path.Combine(tempDownloadDirectory, file));
                        img = XImage.FromStream(imageFile.Encode(SKEncodedImageFormat.Png, 100).AsStream(true));
                    }

                    gfx.DrawImage(img, new XRect(new XPoint(0, 0), new XVector(pageMap[file].Width, pageMap[file].Height)));
                    img.Dispose();
                }

                await using (var fs = new FileStream(outputFilePath, FileMode.Create))
                    document.Save(fs);

                break;
        }

        Directory.Delete(tempDownloadDirectory, true);
        return true;
    }

    private static string GetTempDirectory()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        if (File.Exists(tempDirectory))
            return GetTempDirectory();

        Directory.CreateDirectory(tempDirectory);
        return tempDirectory;
    }

    private static string NormalizePath(string path)
    {
        Span<char> buffer = stackalloc char[path.Length];
        var invalidChars = Path.GetInvalidFileNameChars();
        var hasInvalid = false;
        for (var i = 0; i < path.Length; i++)
            if (invalidChars.Contains(path[i]))
            {
                buffer[i] = '_';
                hasInvalid = true;
            }
            else
                buffer[i] = path[i];

        return hasInvalid ? new string(buffer) : path;
    }

    private static string ResolvePath(string path) =>
        Path.GetFullPath(path.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
            .Replace("//", Path.DirectorySeparatorChar.ToString())
            .Replace("\\", Path.DirectorySeparatorChar.ToString()));
}

internal static class UriExtensions
{
    internal static Uri Combine(this Uri self, string other) => new(self, other);

    internal static Uri Combine(this Uri self, Uri other) => new(self, other);

    internal static Uri CombineRaw(this Uri self, string other) => new($"{self}{other}");

    internal static Uri CombineRaw(this Uri self, Uri other) => new($"{self}{other}");
}

internal static class EnumDescriptionExtension
{
    public static string GetDescription<T>(this T enumerationValue,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
        Type enumerationType)
        where T : Enum
    {
        if (enumerationValue.GetType() != enumerationType)
            throw new TypeAccessException("Type passed to GetDescription is not equal to type of value passed");
        return enumerationType
            .GetField(enumerationValue.ToString())
            ?.GetCustomAttributes(typeof(DescriptionAttribute), false)
            .FirstOrDefault() is DescriptionAttribute attribute
            ? attribute.Description
            : enumerationValue.ToString();
    }

    public static string GetMylarDescription<T>(this T enumerationValue,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
        Type enumerationType)
        where T : Enum
    {
        if (enumerationValue.GetType() != enumerationType)
            throw new TypeAccessException("Type passed to GetMylarDescription is not equal to type of value passed");
        return enumerationType
            .GetField(enumerationValue.ToString())
            ?.GetCustomAttributes(typeof(MylarDescriptionAttribute), false)
            .FirstOrDefault() is MylarDescriptionAttribute attribute
            ? attribute.Description
            : enumerationValue.ToString();
    }
}

public static class EnumerableExtensions
{
    public static IEnumerable<T> NullToEmpty<T>(this IEnumerable<T>? src) => src ?? [];
    public static T[]? EmptyToNull<T>(this T[] src) => src.Length == 0 ? null : src;
}
