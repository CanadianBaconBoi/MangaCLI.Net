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

using AniListNet;
using AniListNet.Objects;

namespace MangaCLI.Net.Models;

public interface IComic
{
    public string Title { get; init; }
    public string Identifier { get; init; }
    public string Slug { get; init; }
    public string? Description { get; init; }
    public string? CoverThumbnail { get; init; }
    public string? CoverUrl { get; init; }
    public string AnilistId { get; }
    public Media? AnilistInfo { get; }
    public AniPagination<StaffEdge>? AnilistStaff { get; }
    public AniPagination<CharacterEdge>? AnilistCharacters { get; }
    public AniPagination<MediaReview>? AnilistReviews { get; }


    public IEnumerable<IChapter> GetChapters(string language);

    public ComicInfo ComicInfo { get; }
}