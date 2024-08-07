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

namespace MangaLib.Net.Base.Models;

public interface IComic
{
    public string Title { get; init; }
    public string Identifier { get; init; }
    public string Slug { get; init; }
    public string? Description { get; init; }
    public string? CoverThumbnail { get; init; }
    public string? CoverUrl { get; init; }
    public IAsyncEnumerable<IChapter> GetChapters(string language);
    public Dictionary<string, string>? GetMetadataIdentifiers();
    public ComicInfo RawComicInfo { get; }
}