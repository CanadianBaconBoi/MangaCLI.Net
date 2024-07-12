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

#pragma warning disable CS8618
class CommandLineOptions
{
    public CommandLineOptions()
    { }
    [Option('q', "query",
        Required = true,
        HelpText = "Search query to use when finding manga")]
    public string SearchQuery { get; set; }
    [Option('m', "manga",
        Default = SearchSelectionType.First,
        HelpText = "Which manga to select from search (First/Random/Exact)")]
    public SearchSelectionType SearchSelection { get; set; }
    [Option('S', "source",
        Default = "ComicK",
        HelpText = "The source to download manga from (ComicK/...)")]
    public string Source { get; set; }
    [Option('G', "group",
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
}

public enum SearchSelectionType
{
    First, Random, Exact
}

public enum OutputFormat
{
    PDF, CBZ
}

#pragma warning restore CS8618
