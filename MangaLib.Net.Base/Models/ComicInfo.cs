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
using System.ComponentModel.DataAnnotations;
using MangaLib.Net.Base.Helpers;
using MangaLib.Net.Base.Metadata;

namespace MangaLib.Net.Base.Models;

// ReSharper disable InconsistentNaming
public class ComicInfo
{
    public long Identifier;

    public string[]? Authors;
    
    public string[]? Artists
    {
        get =>
            Penciller.NullToEmpty()
                .Concat(Inker.NullToEmpty())
                .Concat(Colorist.NullToEmpty())
                .Concat(Letterer.NullToEmpty())
                .Concat(CoverArtist.NullToEmpty())
                .Concat(Editors.NullToEmpty()).ToArray();
        set => Penciller = value;
    }
    
    public string[]? Penciller;
    public string[]? Inker;
    public string[]? Colorist;
    public string[]? Letterer;
    public string[]? CoverArtist;
    public string[]? Editors;
    
    public string[]? Publishers;

    public string? Title;
    public string? Country;
    public StatusType? Status;
    public Dictionary<string, string>? Links;
    public float? TotalChapters;
    public int? TotalVolumes;
    public string? Description;
    public string? DescriptionHtml;
    public DateOnly? StartDate;
    public DateOnly? EndDate;

    [Range(0, 5)]
    public float? CommunityRating;
    public AgeRatingType? AgeRating;
    public Dictionary<string, string>? AlternateTitles;
    public string[]? Genres;
    public string[]? Tags;
    public IEnumerable<(string, ImageType)>? Covers;

    public string? Review;

    public string[]? Characters;

    public enum AgeRatingType
    {
        [MylarDescription("All")] [Description("Unknown")]
        Unknown,

        [MylarDescription("Adult")] [Description("Adults Only 18+")]
        AdultsOnly18,

        [MylarDescription("All")] [Description("Early Childhood")]
        EarlyChildhood,

        [MylarDescription("All")] [Description("Everyone")]
        Everyone,

        [MylarDescription("9+")] [Description("Everyone 10+")]
        Everyone10,

        [MylarDescription("All")] [Description("G")]
        G,

        [MylarDescription("All")] [Description("Kids to Adults")]
        KidsToAdults,

        [MylarDescription("Adult")] [Description("M")]
        M,

        [MylarDescription("15+")] [Description("MA15+")]
        MA15,

        [MylarDescription("17+")] [Description("Mature 17+")]
        Mature17,

        [MylarDescription("12+")] [Description("PG")]
        PG,

        [MylarDescription("Adult")] [Description("R18+")]
        R18,

        [MylarDescription("All")] [Description("Rating Pending")]
        RatingPending,

        [MylarDescription("15+")] [Description("Teen")]
        Teen,

        [MylarDescription("Adult")] [Description("X18+")]
        X18
    }

    public enum StatusType
    {
        [MylarDescription("Ended")] [Description("Ended")]
        Ended,

        [MylarDescription("Continuing")] [Description("Continuing")]
        Continuing,

        [MylarDescription("Continuing")] [Description("Unknown")]
        Unknown
    }

    public struct ImageType
    {
        public int Width;
        public int Height;
        public Uri Location;
    }
}