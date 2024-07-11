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
using MimeTypes;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SixLabors.ImageSharp;

namespace MangaCLI.Net;

static class MangaCli
{
    
    public static IConnector Connector = null!;
    public static void Main(string[] args)
    {
        var optionsOptional = Parser.Default.ParseArguments<CommandLineOptions>(args);
        if (optionsOptional.Errors.Any())
        {
            foreach (var error in optionsOptional.Errors)
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
            }
        }

        var options = optionsOptional.Value;
        
        var outputDirectory = options.OutputFolder
            .Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)).Replace("//", "/")
            .Replace("\\", "/");

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }
        
        switch (options.Source.ToLower())
        {
            case "comick":
                Connector = new Manga.ComicK.ComickConnector();
                break;
            default:
                Console.WriteLine(HelpText.AutoBuild(optionsOptional, t => t, e => e));
                Console.WriteLine($"Source \"{options.Source}\" does not exist");
                Environment.Exit(1);
                return;
        }

        var comics = Connector.SearchComics(options.SearchQuery);
        IComic? comic = null;
        switch (options.SearchSelection)
        {
            case SearchSelectionType.Exact:
                if (options.SearchQuery == options.SearchQuery.ToLower())
                    options.SearchQuery = string.Concat(options.SearchQuery[0].ToString().ToUpper(), options.SearchQuery.AsSpan(1));
                comic = comics.FirstOrDefault(c => c.Title.Equals(options.SearchQuery));
                break;
            case SearchSelectionType.First:
                comic = comics.FirstOrDefault();
                break;
            case SearchSelectionType.Random:
                var comicsArray = comics.ToArray();
                comic = comicsArray.Length == 0 ? null : comicsArray[Random.Shared.Next(0, comicsArray.Length - 1)];
                break;
        }

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
            Console.Error.WriteLine("No chapters found");
            Environment.Exit(1);
            return;
        }
        
        if (options.DoSubfolder)
        {
            outputDirectory = Path.Combine(outputDirectory,
                String.Join("_", comic.Title.Split(Path.GetInvalidFileNameChars())));
            Directory.CreateDirectory(outputDirectory);
        }

        Dictionary<string, IChapter> chaptersToTake = [];

        foreach (var chapter in chapters)
        {
            if(chaptersToTake.ContainsKey(chapter.ChapterIndex ?? "0"))
                continue;
            if (chapter.GroupName == null || !chapter.GroupName.Contains(options.ScanlationGroup))
            {
                var foundProperChapter = false;
                foreach (var otherChapter in chapters)
                    if (otherChapter.GroupName != null && chapter.ChapterIndex == otherChapter.ChapterIndex &&
                        otherChapter.GroupName.Contains(options.ScanlationGroup))
                    {
                        foundProperChapter = true;
                        break;
                    }
                if(foundProperChapter)
                    continue;
            }
            if (String.IsNullOrEmpty(chapter.Title))
            {
                foreach (var otherChapter in chapters)
                    if (chapter.ChapterIndex == otherChapter.ChapterIndex && !String.IsNullOrEmpty(otherChapter.Title))
                        chapter.Title = otherChapter.Title;
                if (String.IsNullOrEmpty(chapter.Title))
                    chapter.Title = $"Chapter {chapter.ChapterIndex ?? "0"}";
            }
            chaptersToTake[chapter.ChapterIndex ?? "0"] = chapter;
        }

        var comicInfo = chapters.First().GetComicInfo();
        var mylarSeriesPath = Path.Combine(outputDirectory, "series.json");
        if (!File.Exists(mylarSeriesPath) || (File.Exists(mylarSeriesPath) && options.Overwrite))
            using (var fs = new FileStream(Path.Combine(outputDirectory, "series.json"), FileMode.Create))
                JsonSerializer.Serialize(fs, new MylarSeries
                {
                    Metadata = new MylarSeries.MylarMetadata
                    {
                        Type = "comicSeries",
                        AgeRating = comicInfo.AgeRating.GetMylarDescription(),
                        BookType = "Print",
                        ComicId = comicInfo.Identifier,
                        Year = comicInfo.Year ?? DateTime.Now.Year,
                        CoverImageUrl = comicInfo.CoverUrl ?? chapters.First().GetPages().ToArray().First().Url,
                        TotalIssues = comicInfo.Count ?? chaptersToTake.Count,
                        Description = comic.Description?.ReplaceLineEndings("") ?? "",
                        Name = comic.Title,
                        DescriptionHtml = null,
                        Volume = null,
                        Imprint = null,
                        PublicationRun = $"{comicInfo.Year}",
                        Status = comicInfo.Status.GetMylarDescription(),
                        Publisher = comicInfo.Publisher,
                    }
                }, MylarJsonContext.Default.Options);

        foreach (var (_, chapter) in chaptersToTake)
        {
            if (chapter.GroupName == null || chapter.GroupName.Length == 0)
                chapter.GroupName = ["UNKNOWN"];
        }

        var chapterCounter = 1;
        foreach (var (chapterIndex, chapter) in chaptersToTake)
        {
            var outputFilePath = Path.Combine(
                outputDirectory,
                String.Join("_", $"[{chapterCounter++:0000}]_Chapter_{chapterIndex}_[{chapter.GroupName!.First()}].{
                    options.Format switch {
                        OutputFormat.CBZ => "cbz",
                        OutputFormat.PDF => "pdf",
                        _ => throw new NotSupportedException($"Format {options.Format} is not supported for file output")
                    }
                }"
                    .Split(Path.GetInvalidFileNameChars())
                )
                );
            
            if (File.Exists(outputFilePath))
            {
                if (!options.Overwrite)
                    continue;
                File.Delete(outputFilePath);
            }
            
            Console.Write("\r" + new string(' ', Console.BufferWidth) + "\r");
            Console.Write($"Downloading Chapter {chapterIndex} : {chapter.Title}");
            
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
                    continue;
            }
            
            switch (options.Format)
            {
                case OutputFormat.CBZ:
                    using (var fs = new FileStream(
                               Path.Combine(tempDownloadDirectory,
                                   "ComicInfo.xml"),
                               FileMode.CreateNew
                           ))
                        ComicInfo.Serializer.Serialize(fs, chapter.GetComicInfo());
            
                    ZipFile.CreateFromDirectory(tempDownloadDirectory, outputFilePath);
                    break;
                case OutputFormat.PDF:
                    var document = new PdfDocument();
                    document.Language = comicInfo.LanguageISO;
                    document.Options.ColorMode = PdfColorMode.Rgb;
                    document.Info.Title = comicInfo.Title;
                    document.Info.Author = comicInfo.Writer;
                    document.Info.Creator = "MangaCLI.Net";
                    document.Info.Subject = comicInfo.Series;
                    
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
                default:
                    throw new NotSupportedException($"Format {options.Format} is not supported for file output");
            }
            Directory.Delete(tempDownloadDirectory, true);
        }
        Console.WriteLine($"\nDownloaded Manga: {comic.Title} [{chaptersToTake.Count} Chapters]");
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