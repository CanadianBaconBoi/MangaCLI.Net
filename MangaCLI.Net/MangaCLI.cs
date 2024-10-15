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
using System.Reflection;
using System.Text;
using CommandLine;
using Connectors.ComicK;
using MangaCLI.Net.Logging;
using MangaLib.Net.Base.Connectors.Manga;
using MangaLib.Net.Base.Connectors.Metadata;
using MangaLib.Net.Base.Metadata;
using MangaLib.Net.Base.Models;
using MangaLib.Net.Connectors.Manga;
using MangaLib.Net.Connectors.Metadata;
using MangaLib.Net.Models;
using MimeTypes;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Providers;
using SkiaSharp;

namespace MangaCLI.Net;

internal static class MangaCli
{
    private static readonly string ConfigFolderPath = Environment.OSVersion.Platform switch
    {
        PlatformID.Unix => ResolvePath(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config/MangaCLI/")),
        PlatformID.Win32NT => ResolvePath(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MangaCLI/")),
        _ => throw new ArgumentOutOfRangeException(
            $"Operating system {Environment.OSVersion.VersionString} is not supported")
    };
    
    private static ConnectorWrapper? _connector;
    private static Config Config { set; get; } = null!;
    private static readonly List<PluginLoadContext> PluginContexts = new();

    private static readonly Dictionary<string, Assembly> PackagedPlugins = new()
    {
        { "DefaultConnectors", typeof(ComickConnector).Assembly },
        { "DefaultProviders", typeof(AnilistMetadataProvider).Assembly }
    };

    private static ILogger _logger;
    
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CommandLineOptions))]
    public static async Task Main(string[] args)
    {
        if (!Directory.Exists(ConfigFolderPath))
            Directory.CreateDirectory(ConfigFolderPath);
        Config = await Config.FromFile(Path.Combine(ConfigFolderPath, "mangacli.toml"));
        var pluginsDirectory = Path.Combine(ConfigFolderPath, "plugins/");
        if (Directory.Exists(pluginsDirectory))
        {
            foreach (var pluginDirectory in Directory.EnumerateDirectories(pluginsDirectory, "*", SearchOption.TopDirectoryOnly))
            {
                foreach (var file in Directory.EnumerateFiles(pluginDirectory, "*", SearchOption.TopDirectoryOnly))
                {
                    if(!file.EndsWith("Plugin.dll", StringComparison.InvariantCultureIgnoreCase)) continue;
                    var pluginContext = new PluginLoadContext(pluginDirectory);
                    var isValidPlugin = false;
                    try
                    {
                        var assembly = pluginContext.LoadFromAssemblyPath(file);
                        foreach (var type in assembly.GetTypes())
                            if (typeof(IMetadataProvider).IsAssignableFrom(type)
                                && type.GetCustomAttributes(typeof(MetadataProviderDescriptorAttribute), false).Length != 0
                               )
                            {
                                MetadataProviderWrapper.AddExternalProvider(assembly, type);
                                isValidPlugin = true;
                            }
                            else if (typeof(IConnector).IsAssignableFrom(type)
                                     && type.GetCustomAttributes(typeof(ConnectorDescriptorAttribute), false).Length != 0
                                    )
                            {
                                ConnectorWrapper.AddExternalConnector(assembly, type);
                                isValidPlugin = true;
                            }
                    }
                    catch (Exception e) when (e is FileLoadException or BadImageFormatException)
                    {
                        await Console.Out.WriteLineAsync($"Failed to load plugin {pluginDirectory}");
                    }

                    if (isValidPlugin)
                        PluginContexts.Add(pluginContext);
                    else
                        pluginContext.Unload();
                }
            }
        }
        else
            Directory.CreateDirectory(pluginsDirectory);

        foreach (var (name, assembly) in PackagedPlugins)
        {
            try
            {
                foreach (var type in assembly.GetTypes())
                    if (typeof(IMetadataProvider).IsAssignableFrom(type)
                        && type.GetCustomAttributes(typeof(MetadataProviderDescriptorAttribute), false).Length != 0)
                        MetadataProviderWrapper.AddExternalProvider(assembly, type);
                    else if (typeof(IConnector).IsAssignableFrom(type) 
                             && type.GetCustomAttributes(typeof(ConnectorDescriptorAttribute), false).Length != 0)
                        ConnectorWrapper.AddExternalConnector(assembly, type);
            }
            catch (Exception e) when (e is FileLoadException or BadImageFormatException)
            {
                await Console.Out.WriteLineAsync($"Failed to load built-in plugin {name}");
            }
        }
        
        var options = await ValidateCommandLine(Parser.Default.ParseArguments<CommandLineOptions>(args));
        
        foreach (var comicName in options.SearchQuery)
        {
            var result = await DownloadComic(options.Clone(), comicName);
            var comicInfo = await result.Comic!.GetComicInfo(Config.MainPriorities, Config.PriorityOverrides);
            switch (result.Result)
            {
                case DownloadResult.NotFound:
                    await _logger.LogError($"No comic found for \"{comicName}\"");
                    break;
                case DownloadResult.NoChapters:
                    await _logger.LogError($"No chapters found for comic \"{result.Comic!.Title}\" ({comicInfo.Title})");
                    break;
                case DownloadResult.NoGoodChapters:
                    await _logger.LogError($"No chapters found for comic \"{result.Comic!.Title}\" ({comicInfo.Title}) with Scanlation Group {options.ScanlationGroup}. Try a different Scanlation Group or enable Alternate Groups with --allow-alternate");
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
                    await Console.Out.WriteLineAsync("Error parsing command line arguments");
                    break;
                default:
                    await Console.Out.WriteLineAsync("Unknown error parsing command line arguments");
                    break;
            }

            Environment.Exit(1);
            return null;
        }

        var options = optionsResult.Value;
        
        if (options.SimpleLogging)
            _logger = new SimpleLogger();
        else
            _logger = new TuiLogger();


        // The string sanitizer isn't great on CommandLineParser, but it's a small library so we tolerate it
        
        if (!ConnectorWrapper.TryGetConnector(options.Source, out _connector))
        {
            await Console.Out.WriteLineAsync($"Source \"{options.Source}\" does not exist");
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

    private static async Task<(ComicWrapper? Comic, DownloadResult Result)> DownloadComic(CommandLineOptions options, string comicName)
    {
        var comic = options.ManualSearchSelection ? await DoManualComicSearch(comicName) : await FindComic(comicName, options.SearchSelection);
        if (comic == null)
            return (null, DownloadResult.NotFound);

        var comicInfo = await comic.GetComicInfo(Config.MainPriorities, Config.PriorityOverrides);

        await _logger.Log($"Found Comic: \"{comic.Title}\" ({comicInfo.Title})");

        var chapters = comic.GetChapters(options.Language).GetAsyncEnumerator();
        if (!await chapters.MoveNextAsync())
            return (comic, DownloadResult.NoChapters);

        var filteredChapters= await FilterChapters(chapters, options.ScanlationGroup, !options.DisallowAlternateGroups, Config.IgnoredScanlationGroups);
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
                await MetadataMylar.SerializeAsync(fs,
                    MetadataMylar.FromComicInfo(comicInfo,() => filteredChapters.First().Value.Pages.First().Url));


        if (comicInfo.Covers?.FirstOrDefault() is { } cover)
        {
            var response = await MangaLib.Net.Base.MangaLib.GetClient().GetAsync(cover.Item2.Location);
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

            if (!await DownloadChapter(comicInfo, chapter, filteredChapters.Count, Path.Combine(
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

        await _logger.LogWithProgress($"Downloaded Manga: \"{comic.Title}\" ({comicInfo.Title})", filteredChapters.Count - failedChapterCount, filteredChapters.Count, false);

        await Task.Delay(1000);
        return (comic, DownloadResult.Successful);
    }

    private static async Task<ComicWrapper?> FindComic(string searchQuery, SearchSelectionType searchType)
    {
        if (searchQuery == searchQuery.ToLower())
            searchQuery = string.Concat(searchQuery[0].ToString().ToUpper(), searchQuery.AsSpan(1));

        var comics = _connector!.SearchComics(searchQuery);

        return searchType switch
        {
            SearchSelectionType.Exact => await comics.FirstOrDefaultAsync(c => c != null && c.Title.Equals(searchQuery)) is { } comic ? new ComicWrapper(comic) : null,
            SearchSelectionType.First => await comics.FirstOrDefaultAsync() is { } comic ? new ComicWrapper(comic) : null,
            SearchSelectionType.Random => await comics.ToArrayAsync() is { Length: > 0 } comicArray
                ? comicArray[Random.Shared.Next(0, comicArray.Length)] is { } comic ? new ComicWrapper(comic) : null
                : null,
            _ => null
        };
    }
    
    private static async Task<ComicWrapper?> DoManualComicSearch(string searchQuery)
    {
        if (searchQuery == searchQuery.ToLower())
            searchQuery = string.Concat(searchQuery[0].ToString().ToUpper(), searchQuery.AsSpan(1));

        var comics = await _connector!.SearchComics(searchQuery).ToArrayAsync();

        if (comics.Length == 0)
            return null;

        var comic =
            comics[
                await _logger.LogOptions("Select a comic", comics.Select(c => c!.Title).ToArray())
            ];

        return comic is { } ? new ComicWrapper(comic) : null;
    }

    private static async Task<Dictionary<string, IChapter>> FilterChapters(IAsyncEnumerator<IChapter> chapters, string preferredGroup, bool useAlternateGroup, List<string>? ignoredGroups)
    {
        var strCmp = StringComparer.InvariantCultureIgnoreCase;
        Dictionary<string, IChapter> chaptersToTake = new();
        do
        {
            var chapter = chapters.Current;
            if(chapter.GroupName?.Any(g => ignoredGroups?.Contains(g) ?? false) ?? false)
                continue;
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
    
    private static async Task<bool> DownloadChapter(ComicInfoWrapper comicInfoWrapper, IChapter chapter, int maxChapters, string outputFilePath, OutputFormat outputFormat, bool overwrite)
    {
        if (!overwrite && File.Exists(outputFilePath))
            return true;

        var comickRackMetadata = chapter.GetComicRackMetadata(comicInfoWrapper);

        await _logger.ClearLine();
        await _logger.LogWithProgress($"Downloading Chapter {chapter.Title}", int.Parse(chapter.ChapterIndex?.Split('.')[0] ?? "0"), maxChapters, false, false);

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
                            using var response = await MangaLib.Net.Base.MangaLib.GetClient().GetAsync(page.Url, token);
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
                            if (failCount == 3)
                            {
                                await _logger.LogError(
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
                            await _logger.LogError(
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
                    MetadataComicRack.Serialize(mdFs, comickRackMetadata);
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