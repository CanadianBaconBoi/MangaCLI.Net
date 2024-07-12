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
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text.Json;
using CommandLine;
using CommandLine.Text;
using MangaCLI.Net.Manga;
using MangaCLI.Net.Manga.ComicK;
using MimeTypes;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SixLabors.ImageSharp;

namespace MangaCLI.Net;

static class MangaCli
{
    
    public static IConnector Connector = null!;

    private static readonly Dictionary<string, Func<IConnector>> Connectors = new()
    {
        { "ComicK", () => new ComickConnector() }
    };
    
    public static void Main(string[] args)
    {
        var options = ValidateCommandLine(Parser.Default.ParseArguments<CommandLineOptions>(args));
        
        Connector = Connectors.First(connector => options.Source.Equals(connector.Key, StringComparison.CurrentCultureIgnoreCase)).Value();
        
        var comic = FindComic(options.SearchQuery, options.SearchSelection);
        if (comic == null)
        {
            Console.Error.WriteLine($"No comic found for \"{options.SearchQuery}\"");
            Environment.Exit(1);
            return;
        }
        Console.WriteLine($"Found Comic: {comic.Title}");
        
        var chapters = comic.GetChapters(options.Language).ToArray();
        if (chapters.Length == 0)
        {
            Console.Error.WriteLine($"No chapters found for comic \"{comic.Title}\"");
            Environment.Exit(1);
            return;
        }
        
        var filteredChapters = FilterChapters(chapters, options.ScanlationGroup, !options.DisallowAlternateGroups);
        if (filteredChapters.Count == 0)
        {
            Console.Error.WriteLine($"No chapters found for comic \"{comic.Title}\" with Scanlation Group {options.ScanlationGroup}");
            Console.Error.WriteLine($"Try a different Scanlation Group or enable Alternate Groups with --allow-alternate");
            Environment.Exit(1);
            return;
        }
        
        if (!options.NoSubfolder)
        {
            options.OutputFolder = Path.Combine(options.OutputFolder,
                String.Join("_", comic.Title.Split(Path.GetInvalidFileNameChars())));
            Directory.CreateDirectory(options.OutputFolder);
        }
        
        
        var mylarSeriesPath = Path.Combine(options.OutputFolder, "series.json");
        if (!File.Exists(mylarSeriesPath) || (File.Exists(mylarSeriesPath) && options.Overwrite))
            using (var fs = new FileStream(Path.Combine(options.OutputFolder, "series.json"), FileMode.Create))
                JsonSerializer.Serialize(fs,
                    MetadataMylar.FromComicInfo(comic.ComicInfo, () => filteredChapters.First().Value.GetPages().First().Url),
                    MylarJsonContext.Default.Options);
        
        var fileIndex = 1;
        foreach (var (chapterIndex, chapter) in filteredChapters)
        {
            if (chapter.GroupName == null || chapter.GroupName.Length == 0)
                chapter.GroupName = ["UNKNOWN"];
            
            DownloadChapter(chapter, Path.Combine(
                options.OutputFolder,
                String.Join("_", $"[{fileIndex++:0000}]_Chapter_{chapterIndex}_[{chapter.GroupName.First()}].{
                    options.Format switch {
                        OutputFormat.CBZ => "cbz",
                        OutputFormat.PDF => "pdf",
                        _ => throw new ArgumentOutOfRangeException(options.Format.ToString(), $"{options.Format.ToString()} is not a valid output format") }
                }"
                    .Split(Path.GetInvalidFileNameChars())
                )
            ), options.Format, options.Overwrite);
        }
        Console.WriteLine($"\nDownloaded Manga: {comic.Title} [{filteredChapters.Count} Chapters]");
    }

    private static CommandLineOptions ValidateCommandLine(ParserResult<CommandLineOptions> optionsResult)
    {
        foreach (var error in optionsResult.Errors)
        {
            switch (error.Tag)
            {
                case ErrorType.UnknownOptionError:
                    Console.Error.WriteLine("Unknown command line argument");
                    break;
                case ErrorType.RepeatedOptionError:
                    Console.Error.WriteLine("Command line argument specified multiple times");
                    break;
                case ErrorType.MissingRequiredOptionError:
                    Console.Error.WriteLine("Required command line argument not present");
                    break;
                default:
                    Console.Error.WriteLine("Error parsing command line arguments");
                    break;
            }

            Environment.Exit(1);
            return null;
        }

        var options = optionsResult.Value;

        if (!Connectors.Any(pair => options.Source.Equals(pair.Key, StringComparison.CurrentCultureIgnoreCase)))
        {
            Console.WriteLine(HelpText.AutoBuild(optionsResult, t => t, e => e));
            Console.WriteLine($"Source \"{options.Source}\" does not exist");
            Environment.Exit(1);
            return null;
        }
        
        options.OutputFolder = options.OutputFolder
            .Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)).Replace("//", "/")
            .Replace("\\", "/");
        
        if (!Directory.Exists(options.OutputFolder))
            Directory.CreateDirectory(options.OutputFolder);
        
        return options;
    }

    private static IComic? FindComic(string searchQuery, SearchSelectionType searchType)
    {
        var comics= Connector.SearchComics(searchQuery).ToArray();
        if (searchQuery == searchQuery.ToLower())
            searchQuery = string.Concat(searchQuery[0].ToString().ToUpper(), searchQuery.AsSpan(1));
        
        return searchType switch
        {
            SearchSelectionType.Exact => comics.FirstOrDefault(c => c.Title.Equals(searchQuery)),
            SearchSelectionType.First => comics.FirstOrDefault(),
            SearchSelectionType.Random => comics.ElementAt(Random.Shared.Next(0, comics.Length)),
            _ => null
        };
    }

    private static Dictionary<string, IChapter> FilterChapters(IChapter[] chapters, string preferredGroup, bool useAlternateGroup)
    {
        Dictionary<string, IChapter> chaptersToTake = new();
        
        foreach (var chapter in chapters)
        {
            if(chaptersToTake.ContainsKey(chapter.ChapterIndex ?? "0"))
                continue;
            if (chapter.GroupName == null || !chapter.GroupName.Contains(preferredGroup))
            {
                var foundProperChapter = false;
                foreach (var otherChapter in chapters)
                    if (otherChapter.GroupName != null && chapter.ChapterIndex == otherChapter.ChapterIndex &&
                        otherChapter.GroupName.Contains(preferredGroup))
                    {
                        foundProperChapter = true;
                        break;
                    }
                if(foundProperChapter || !useAlternateGroup)
                    continue;
            }
            if (string.IsNullOrEmpty(chapter.Title))
            {
                foreach (var otherChapter in chapters)
                    if (chapter.ChapterIndex == otherChapter.ChapterIndex && !String.IsNullOrEmpty(otherChapter.Title))
                        chapter.Title = otherChapter.Title;
                if (string.IsNullOrEmpty(chapter.Title))
                    chapter.Title = $"Chapter {chapter.ChapterIndex ?? "0"}";
            }
            chaptersToTake[chapter.ChapterIndex ?? "0"] = chapter;
        }

        return chaptersToTake;
    }

    private static void DownloadChapter(IChapter chapter, string outputFilePath, OutputFormat outputFormat, bool overwrite)
    {
        var comickRackMetadata = chapter.GetComicRackMetadata();

        if (File.Exists(outputFilePath))
        {
            if (!overwrite)
                return;
            File.Delete(outputFilePath);
        }
        
        Console.Write("\r" + new string(' ', Console.BufferWidth) + "\r");
        Console.Write($"Downloading Chapter {chapter.ChapterIndex} : {chapter.Title}");
        
        string tempDownloadDirectory = GetTempDirectory();
        
        var pages = chapter.GetPages();
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
                        {
                            try
                            {
                                var response = await Connector.GetClient().GetAsync(page.Url, token);
                                if (response.StatusCode != HttpStatusCode.OK)
                                    throw new Exception("Incorrect Status Code");
                                var path = Path.Combine(tempDownloadDirectory,
                                    $"{i + 1:000000}{MimeTypeMap.GetExtension(response.Content.Headers.ContentType?.MediaType ?? "image/jpeg")}");
                                pageMap[path] = page;
                                await using var fs = new FileStream(path, FileMode.CreateNew);
                                await response.Content.CopyToAsync(fs, token);
                                break;
                            }
                            catch
                            {
                                failCount++;
                                if (failCount == 1)
                                    Console.WriteLine();
                                if (failCount == 3)
                                {
                                    Console.Error.WriteLine(
                                        $"Failed to request page {i + 1} three times, chapter {chapter.ChapterIndex} will be missing.");
                                    await cts.CancelAsync();
                                    break;
                                }

                                var sleep = failCount switch
                                {
                                    1 => 1,
                                    2 => 10
                                };
                                Console.Error.WriteLine(
                                    $"Failed to request page {i + 1}, retrying in {sleep} seconds.");
                                await Task.Delay(sleep * 1000, token);
                            }
                        }
                    }).GetAwaiter().GetResult();
            }
            catch {}


            if (cts.IsCancellationRequested)
                return;
        }
        
        switch (outputFormat)
        {
            case OutputFormat.CBZ:
                using (var fs = new FileStream(
                           Path.Combine(tempDownloadDirectory,
                               "ComicInfo.xml"),
                           FileMode.CreateNew
                       ))
                    MetadataComicRack.Serializer.Serialize(fs, comickRackMetadata);
        
                ZipFile.CreateFromDirectory(tempDownloadDirectory, outputFilePath);
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
                    page.Width = pageMap[file].Width;
                    page.Height = pageMap[file].Height;
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
                    gfx.DrawImage(img, 0, 0, pageMap[file].Width, pageMap[file].Height);
                    img.Dispose();
                }
                document.Save(outputFilePath);
                break;
        }
        Directory.Delete(tempDownloadDirectory, true);
    }
    
    private static string GetTempDirectory()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        if(File.Exists(tempDirectory)) {
            return GetTempDirectory();
        }

        Directory.CreateDirectory(tempDirectory);
        return tempDirectory;
    }
}

internal static class UriExtensions
{
    internal static Uri Combine(this Uri self, string other) => new Uri(self, other);
    internal static Uri Combine(this Uri self, Uri other) => new Uri(self, other);
    internal static Uri CombineRaw(this Uri self, string other) => new Uri($"{self}{other}");
    internal static Uri CombineRaw(this Uri self, Uri other) => new Uri($"{self}{other}");
}

internal static class EnumExtensions
{
    public static string GetDescription<T>(this T enumerationValue)
        where T : Enum
    {
        Type type = enumerationValue.GetType();
        MemberInfo[] memberInfo = type.GetMember(enumerationValue.ToString());
        if (memberInfo.Length > 0)
        {
            object[] attrs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (attrs.Length > 0)
                return ((DescriptionAttribute)attrs[0]).Description;
        }
        return enumerationValue.ToString();
    }
    
    public static string GetMylarDescription<T>(this T enumerationValue)
        where T : Enum
    {
        Type type = enumerationValue.GetType();
        MemberInfo[] memberInfo = type.GetMember(enumerationValue.ToString());
        if (memberInfo.Length > 0)
        {
            object[] attrs = memberInfo[0].GetCustomAttributes(typeof(MylarDescriptionAttribute), false);
            if (attrs.Length > 0)
                return ((MylarDescriptionAttribute)attrs[0]).Description;
        }
        return enumerationValue.ToString();
    }
}