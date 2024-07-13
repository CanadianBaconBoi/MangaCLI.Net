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

namespace MangaCLI.Net.Manga.ComicK.Models;

public class ComickChapterWrapper
{
#pragma warning disable CS8618
    [JsonPropertyName("chapter")] public ComickFatChapter Chapter { get; init; }

    public class ComickFatChapter
    {
        [JsonPropertyName("images")] public ComickPage[] Pages { get; init; }
    }
#pragma warning restore CS8618
}