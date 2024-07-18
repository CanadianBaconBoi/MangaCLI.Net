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

using System.Reflection;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using MangaLib.Net.Models;
using Tomlyn;

namespace MangaCLI.Net;

public class Config
{
    [IgnoreDataMember] private static readonly string DefaultToml = $"""
                                                              # {Assembly.GetExecutingAssembly().GetName().Name} {Assembly.GetExecutingAssembly().GetName().Version} Config

                                                              priorities = ["anilist", "connector"]

                                                              # The below priority overrides apply to the individual
                                                              # data points. These can be set independently of the
                                                              # above priorities and are used to select better data
                                                              # with higher granularity.

                                                              [priority_overrides]
                                                                  identifier          = []
                                                                  authors             = []
                                                                  artists             = []
                                                                  penciller           = []
                                                                  inker               = []
                                                                  colorist            = []
                                                                  letterer            = []
                                                                  cover_artist        = []
                                                                  editors             = []
                                                                  publishers          = []
                                                                  title               = []
                                                                  country             = []
                                                                  status              = []
                                                                  links               = []
                                                                  total_chapters      = []
                                                                  total_volumes       = []
                                                                  description         = []
                                                                  description_html    = []
                                                                  start_date          = []
                                                                  end_date            = []
                                                                  community_rating    = []
                                                                  age_rating          = []
                                                                  alternate_titles    = []
                                                                  genres              = []
                                                                  tags                = []
                                                                  covers              = []
                                                                  review              = []
                                                                  characters          = []

                                                              """;

    public static async Task<Config> FromFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                await using var fs = new FileStream(path, FileMode.Open);
                using var fsr = new StreamReader(fs);
                return Toml.ToModel<Config>(await fsr.ReadToEndAsync());
            }

            await WriteDefault(path);
            return await FromFile(path);
        }
        catch (Exception e) when (e is TomlException or SecurityException)
        {
            await Console.Out.WriteLineAsync(e.ToString());
            Environment.Exit(1);
        }

        return null;
    }

    private static async Task WriteDefault(string path)
    {
        await using var fs = new FileStream(path, FileMode.Create);
        await fs.WriteAsync(Encoding.Default.GetBytes(DefaultToml));
    }

    [DataMember(Name = "priorities")]
    public List<String> MainPriorities { get; set; } = [];

    [DataMember(Name = "priority_overrides")]
    public ComicInfoPriorities PriorityOverrides { get; set; } = new();
}