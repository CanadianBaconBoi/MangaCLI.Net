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

using CommandLine;

namespace MangaCLI.Net;

internal class CommandLineOptions
{
#pragma warning disable CS8618
    [Option('q', "query",
        Required = true,
        Group = "query",
        HelpText = "Search query to use when finding manga")]
    public IEnumerable<string> SearchQuery { get; set; }
    
    [Option('Q', "query-file",
        Required = true,
        Group = "query",
        HelpText = "File with line seperated search queries to use when finding manga")]
    public string SearchQueryFile { get; set; }

    [Option('m', "manga",
        Required = true,
        Group = "searchSelection",
        HelpText = "Which manga to select from search (First/Random/Exact)")]
    public SearchSelectionType SearchSelection { get; set; }
    
    [Option('s', "select",
        Required = true,
        Group = "searchSelection",
        HelpText = "Manually select manga from list")]
    public bool ManualSearchSelection { get; set; }

    [Option('S', "source",
        Default = "ComicK",
        HelpText = "The source to download manga from (ComicK/...)")]
    public string Source { get; set; }

    [Option('g', "group",
        Default = "Official",
        HelpText = "Preferred group for scanlations")]
    public string ScanlationGroup { get; set; }

    [Option('O', "output",
        Default = "~/Documents/Manga",
        HelpText = "The folder to download manga into")]
    public string OutputFolder { get; set; }

    [Option('F', "format",
        Default = OutputFormat.CBZ,
        HelpText = "The format to download manga as (CBZ/PDF)")]
    public OutputFormat Format { get; set; }

    [Option('l', "language",
        Default = "en",
        HelpText = "Language to use when searching for chapters (en/jp/ko/...)")]
    public string Language { get; set; }
    
    [Option('G', "ignore-group",
        HelpText = "Scanlation groups that can not be downloaded from")]
    public IEnumerable<string> IgnoredGroups { get; set; }
    
    [Option('t', "text-logging",
        Default = false,
        HelpText = "Use simple logging")]
    public bool SimpleLogging { get; set; }

    [Option("no-subfolder",
        Default = false,
        HelpText = "Don't create a subfolder for manga")]
    public bool NoSubfolder { get; set; }

    [Option("overwrite",
        Default = false,
        HelpText = "Overwrite existing manga with the same name")]
    public bool Overwrite { get; set; }

    [Option("disallow-alternate",
        Default = false,
        HelpText = "Disallow the use of alternate scanlation groups for chapter search for missing chapters")]
    public bool DisallowAlternateGroups { get; set; }
#pragma warning restore CS8618
    public CommandLineOptions Clone() => new()
    {
        SearchQuery = SearchQuery,
        SearchSelection = SearchSelection,
        ManualSearchSelection = ManualSearchSelection,
        Source = Source,
        ScanlationGroup = ScanlationGroup,
        OutputFolder = OutputFolder,
        Format = Format,
        Language = Language,
        NoSubfolder = NoSubfolder,
        Overwrite = Overwrite,
        DisallowAlternateGroups = DisallowAlternateGroups
    };
}

public enum SearchSelectionType
{
    First,
    Random,
    Exact
}

// ReSharper disable InconsistentNaming
public enum OutputFormat
{
    PDF,
    CBZ
}