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

using System.Xml.Serialization;

namespace MangaCLI.Net.Manga;

#pragma warning disable CS8618
public class MetadataComicRack
{
    public static XmlSerializer Serializer = XmlSerializer.FromTypes([typeof(MetadataComicRack)]).First()!;
    
    
    public string Title;
    public string Series;
    public string Number;
    public int? Count;
    public bool ShouldSerializeCount() 
    {
        return Count.HasValue;
    }
    public int? Volume;
    public bool ShouldSerializeVolume() 
    {
        return Volume.HasValue;
    }
    public string AlternateSeries;
    public string AlternateNumber;
    public int? AlternateCount;
    public bool ShouldSerializeAlternateCount() 
    {
        return AlternateCount.HasValue;
    }
    public string Summary;
    public string Notes;
    public int? Year;
    public bool ShouldSerializeYear() 
    {
        return Year.HasValue;
    }
    public int? Month;
    public bool ShouldSerializeMonth() 
    {
        return Month.HasValue;
    }
    public int? Day;
    public bool ShouldSerializeDay() 
    {
        return Day.HasValue;
    }
    public string Writer;
    public string Penciller;
    public string Inker;
    public string Colorist;
    public string Letterer;
    public string CoverArtist;
    public string Editor;
    public string Publisher;
    public string Imprint;
    public string Genre;
    public string Tags;
    public string Web;
    public int PageCount;
    public string LanguageISO;
    public string Format;
    public YesNoType BlackAndWhite;
    public MangaType Manga;
    public string Characters;
    public string Teams;
    public string Locations;
    public string ScanInformation;
    public string StoryArc;
    public string SeriesGroup;

    [XmlElement("AgeRating")]
    public string InternalAgeRating
    {
        get => AgeRating.GetDescription();
        set {}
    }

    [XmlIgnore]
    public ComicInfo.AgeRatingType AgeRating { get; set; }

    private float _communityRating;
    public float CommunityRating
    {
        set
        {
            if (value is >= 0 and <= 5)
                _communityRating = MathF.Round(value, 2);
        }
        get => _communityRating;
    }

    public string MainCharacterOrTeam;
    public string Review;
    public ComicPageInfo[] Pages;

    public enum MangaType
    {
        Unknown, No, Yes, YesAndRightToLeft
    }
    
    public enum YesNoType
    {
        Unknown, No, Yes
    }
    
    public class ComicPageInfo
    {
        public int Image;
        public ComicPageType Type;
        public bool DoublePage;
        public long ImageSize;
        public string Key;
        public string Bookmark;
        public int ImageWidth;
        public int ImageHeight;
    }

    public enum ComicPageType
    {
        FrontCover,
        InnerCover,
        Roundup,
        Story,
        Advertisement,
        Editorial,
        Letters,
        Preview,
        BackCover,
        Other,
        Deleted
    }
}
#pragma warning restore CS8618