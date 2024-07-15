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
    private static readonly AniClient AnilistClient = new();
    
    public abstract string AnilistId { get; }
    
    private Media? _anilistInfo;
    private bool _infoAvailable = true;
    public Media? AnilistInfo
    {
        get
        {
            if (!_infoAvailable)
                return null;
            try
            {
                return _anilistInfo ??= AnilistClient.GetMediaAsync(
                    int.Parse(AnilistId)
                ).GetAwaiter().GetResult();
            }
            catch
            {
                _infoAvailable = false;
                return null;
            }
        }
    }

    private AniPagination<StaffEdge>? _anilistStaff;
    private bool _staffAvailable = true;

    public AniPagination<StaffEdge>? AnilistStaff
    {
        get
        {
            if(!_staffAvailable)
                return null;
            try
            {
                return _anilistStaff ??= AnilistInfo?.GetStaffAsync().GetAwaiter().GetResult();
            }
            catch
            {
                _staffAvailable = false;
                return null;
            }
        }
    }

    private AniPagination<CharacterEdge>? _anilistCharacters;
    private bool _charactersAvailable = true;

    public AniPagination<CharacterEdge>? AnilistCharacters
    {
        get
        {
            if(!_charactersAvailable)
                return null;
            try
            {
                return _anilistCharacters ??= AnilistInfo?.GetCharactersAsync().GetAwaiter().GetResult();
            }
            catch
            {
                _charactersAvailable = false;
                return null;
            }
        }
    }

    private AniPagination<MediaReview>? _anilistReviews;
    private bool _reviewsAvailable = true;

    public AniPagination<MediaReview>? AnilistReviews
    {
        get
        {
            if(!_reviewsAvailable)
                return null;
            try
            {
                return _anilistReviews ??= AnilistInfo?.GetReviewsAsync().GetAwaiter().GetResult();
            }
            catch
            {
                _reviewsAvailable = false;
                return null;
            }
        }
    }
}