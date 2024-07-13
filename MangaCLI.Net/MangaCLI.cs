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
using System.Text.Json;
using AniListNet;
using CommandLine;
using MangaCLI.Net.Manga;
using MangaCLI.Net.Manga.ComicK;
using MimeTypes;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace MangaCLI.Net;

internal static class MangaCli
{
    public static IConnector Connector = null!;

    public static AniClient AnilistClient = new();


    private static readonly Dictionary<string, Func<IConnector>> Connectors = new(StringComparer.OrdinalIgnoreCase)
    {
        { "ComicK", () => new ComickConnector() }
    };

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CommandLineOptions))]
    public static void Main(string[] args)
    {
        args = ["-q Overlord", "-m First"];
        
        var options = ValidateCommandLine(Parser.Default.ParseArguments<CommandLineOptions>(args));

        Connector = Connectors[options.Source]();

        var comic = FindComic(options.SearchQuery, options.SearchSelection);
        if (comic == null)
        {
            Console.Error.WriteLine($"No comic found for \"{options.SearchQuery}\"");
            Environment.Exit(1);
            return;
        }

        Console.WriteLine($"Found Comic: \"{comic.Title}\" ({comic.ComicInfo.Title})");

        var chapters = comic.GetChapters(options.Language).GetEnumerator();
        if (!chapters.MoveNext())
        {
            Console.Error.WriteLine($"No chapters found for comic \"{comic.Title}\" ({comic.ComicInfo.Title})");
            Environment.Exit(1);
            return;
        }

        var filteredChapters= FilterChapters(chapters, options.ScanlationGroup, !options.DisallowAlternateGroups);
        if (filteredChapters.Count == 0)
        {
            Console.Error.WriteLine($"No chapters found for comic \"{comic.Title}\" ({comic.ComicInfo.Title}) with Scanlation Group {options.ScanlationGroup}");
            Console.Error.WriteLine($"Try a different Scanlation Group or enable Alternate Groups with --allow-alternate");
            Environment.Exit(1);
            return;
        }

        if (!options.NoSubfolder)
        {
            options.OutputFolder = Path.Combine(options.OutputFolder, NormalizePath(comic.ComicInfo.Title));
            Directory.CreateDirectory(options.OutputFolder);
        }


        var mylarSeriesPath = Path.Combine(options.OutputFolder, "series.json");
        if (options.Overwrite || !File.Exists(mylarSeriesPath))
            using (var fs = new FileStream(mylarSeriesPath, FileMode.Create))
                JsonSerializer.Serialize(fs,
                    MetadataMylar.FromComicInfo(comic.ComicInfo,
                        () => filteredChapters.First().Value.Pages.First().Url),
                    MylarJsonContext.Default.MetadataMylar);


        if (comic.ComicInfo.Covers?.FirstOrDefault() is { } cover)
        {
            var response = Connector.GetClient().GetAsync(cover.Item2.Location).GetAwaiter().GetResult();
            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception("Incorrect Status Code");
            if (response.Content.Headers.ContentType?.MediaType == "image/webp")
            {
                var coverPath = Path.Combine(options.OutputFolder, "cover.png");
                if (options.Overwrite || !File.Exists(coverPath))
                {
                    using var image = Image.Load(response.Content.ReadAsStream());
                    using var fs = new FileStream(coverPath, FileMode.CreateNew);
                    image.Save(fs, PngFormat.Instance);
                }
            }
            else
            {
                var coverPath = Path.Combine(options.OutputFolder, $"cover{MimeTypeMap.GetExtension(response.Content.Headers.ContentType?.MediaType ?? "image/jpeg")}");
                if (options.Overwrite || !File.Exists(coverPath))
                    using (var fs = new FileStream(coverPath, FileMode.Create))
                        response.Content.CopyTo(fs, null, CancellationToken.None);
            }
        }

        var fileIndex = 1;
        var failedChapterCount = 0;
        foreach (var (chapterIndex, chapter) in filteredChapters)
        {
            if (chapter.GroupName == null || chapter.GroupName.Length == 0)
                chapter.GroupName = ["UNKNOWN"];

            if (!DownloadChapter(chapter, Path.Combine(
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

        Console.WriteLine($"\nDownloaded Manga: \"{comic.Title}\" ({comic.ComicInfo.Title}) [{filteredChapters.Count - failedChapterCount}/{filteredChapters.Count} Chapters]");
    }

    private static CommandLineOptions ValidateCommandLine(ParserResult<CommandLineOptions> optionsResult)
    {
        foreach (var error in optionsResult.Errors)
        {
            switch (error.Tag)
            {
                case ErrorType.UnknownOptionError:
                case ErrorType.RepeatedOptionError:
                case ErrorType.MissingRequiredOptionError:
                    break;
                default:
                    Console.Error.WriteLine("Unknown error parsing command line arguments");
                    break;
            }

            Environment.Exit(1);
            return null;
        }

        var options = optionsResult.Value;

        // The string sanitizer isn't great on CommandLineParser, but it's a small library so we tolerate it
        if (!Connectors.ContainsKey(options.Source))
        {
            Console.Error.WriteLine($"Source \"{options.Source}\" does not exist");
            Environment.Exit(1);
            return null;
        }

        options.OutputFolder = options.OutputFolder
            .Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
            .Replace("//", Path.DirectorySeparatorChar.ToString());
        if ('\\' != Path.DirectorySeparatorChar)
            options.OutputFolder = options.OutputFolder.Replace("\\", Path.DirectorySeparatorChar.ToString());

        if (!Directory.Exists(options.OutputFolder))
            Directory.CreateDirectory(options.OutputFolder);

        return options;
    }

    private static IComic? FindComic(string searchQuery, SearchSelectionType searchType)
    {
        var comics = Connector.SearchComics(searchQuery);
        if (searchQuery == searchQuery.ToLower())
            searchQuery = string.Concat(searchQuery[0].ToString().ToUpper(), searchQuery.AsSpan(1));

        return searchType switch
        {
            SearchSelectionType.Exact => comics.FirstOrDefault(c => c.Title.Equals(searchQuery)),
            SearchSelectionType.First => comics.FirstOrDefault(),
            SearchSelectionType.Random => comics.ToArray() is { Length: > 0 } comicArray
                ? comicArray[Random.Shared.Next(0, comicArray.Length)]
                : null,
            _ => null
        };
    }

    private static Dictionary<string, IChapter> FilterChapters(IEnumerator<IChapter> chapters, string preferredGroup,
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
            else if (!useAlternateGroup || chapter.GroupName?.Contains(preferredGroup, strCmp) is true)
                chaptersToTake[chapter.ChapterIndex ?? "0"] = chapter;
        } while (chapters.MoveNext());

        foreach (var chapter in chaptersToTake.Values.Where(chapter => string.IsNullOrEmpty(chapter.Title)))
            chapter.Title = $"Chapter {chapter.ChapterIndex ?? "0"}";

        return chaptersToTake;
    }

    [UnconditionalSuppressMessage("Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "All members are referenced")]
    private static bool DownloadChapter(IChapter chapter, string outputFilePath, OutputFormat outputFormat, bool overwrite)
    {
        if (!overwrite && File.Exists(outputFilePath))
            return true;

        var comickRackMetadata = chapter.GetComicRackMetadata();

        Console.Write("\r" + new string(' ', Console.BufferWidth) + "\r");
        Console.Write($"Downloading Chapter {chapter.ChapterIndex} : {chapter.Title}");

        var tempDownloadDirectory = GetTempDirectory();

        var pages = chapter.Pages;
        var pageMap = new Dictionary<string, IPage>();

        using (var cts = new CancellationTokenSource())
        {
            try
            {
                Parallel.ForAsync(0, pages.Length, new ParallelOptions()
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
                                    using var image = await Image.LoadAsync(await response.Content.ReadAsStreamAsync(token), token);
                                    await using var fs = new FileStream(path, FileMode.CreateNew);
                                    await image.SaveAsync(fs, PngFormat.Instance, token);
                                }
                                else
                                {
                                    var path = Path.Combine(tempDownloadDirectory, $"{i + 1:000000}{MimeTypeMap.GetExtension(response.Content.Headers.ContentType?.MediaType ?? "image/jpeg")}");
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
                                    Console.WriteLine();
                                if (failCount == 3)
                                {
                                    await Console.Error.WriteLineAsync($"Failed to request page {i + 1} three times, chapter {chapter.ChapterIndex} will be missing.");
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
                                await Console.Error.WriteLineAsync($"Failed to request page {i + 1}, retrying in {sleep} seconds.");
                                await Task.Delay(sleep * 1000, token);
                            }
                    }).GetAwaiter().GetResult();
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch { }

            if (cts.IsCancellationRequested)
                return false;
        }

        switch (outputFormat)
        {
            case OutputFormat.CBZ:
                using (var mdFs = new FileStream(Path.Combine(tempDownloadDirectory, "ComicInfo.xml"), FileMode.CreateNew))
                    MetadataComicRack.Serializer.Serialize(mdFs, comickRackMetadata);
                using (var fs = new FileStream(outputFilePath, FileMode.Create))
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
                        using var imageStream = new MemoryStream();
                        using var imageFile = Image.Load(Path.Combine(tempDownloadDirectory, file));
                        imageFile.SaveAsBmp(imageStream);
                        img = XImage.FromStream(imageStream);
                    }

                    gfx.DrawImage(img, new XRect(new XPoint(0, 0), new XVector(pageMap[file].Width, pageMap[file].Height)));
                    img.Dispose();
                }

                using (var fs = new FileStream(outputFilePath, FileMode.Create))
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