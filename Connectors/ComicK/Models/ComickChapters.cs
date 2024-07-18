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

using System.Text.Json.Serialization;
using Connectors.ComicK.Models;

namespace MangaLib.Net.Connectors.Manga.ComicK.Models;

public class ComickChapters
{
#pragma warning disable CS8618
    [JsonPropertyName("chapters")] public ComickChapter[] Chapters { get; init; }

    [JsonPropertyName("total")] public int Total { get; init; }

    [JsonPropertyName("limit")] public int Limit { get; init; }
#pragma warning restore CS8618
}