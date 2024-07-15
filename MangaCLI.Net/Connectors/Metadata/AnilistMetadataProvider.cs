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

namespace MangaCLI.Net.Connectors.Metadata;

public abstract class AnilistMetadataProvider
{
    public static readonly AniClient AnilistClient = new();
    
    public abstract string AnilistId { get; }
    
    private Media? _anilistInfo;
    private static readonly Media EmptyMedia = new Media();
    public Media? AnilistInfo
    {
        get
        {
            if (_anilistInfo == EmptyMedia)
                return null;
            try
            {
                return _anilistInfo ??= AnilistClient.GetMediaAsync(
                    int.Parse(AnilistId)
                ).GetAwaiter().GetResult();
            }
            catch
            {
                _anilistInfo = EmptyMedia;
                return null;
            }
        }
    }

    private AniPagination<StaffEdge>? _anilistStaff;

    public AniPagination<StaffEdge>? AnilistStaff =>
        _anilistStaff ??= AnilistInfo?.GetStaffAsync().GetAwaiter().GetResult();

    private AniPagination<CharacterEdge>? _anilistCharacters;

    public AniPagination<CharacterEdge>? AnilistCharacters =>
        _anilistCharacters ??= AnilistInfo?.GetCharactersAsync().GetAwaiter().GetResult();

    private AniPagination<MediaReview>? _anilistReviews;

    public AniPagination<MediaReview>? AnilistReviews =>
        _anilistReviews ??= AnilistInfo?.GetReviewsAsync().GetAwaiter().GetResult();
}