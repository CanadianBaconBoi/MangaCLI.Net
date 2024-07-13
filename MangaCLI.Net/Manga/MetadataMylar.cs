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

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace MangaCLI.Net.Manga;

public class MetadataMylar
{
    #pragma warning disable CS8618
    [JsonPropertyName("version")]
    public string Version = "1.0.2";
    [JsonPropertyName("metadata")]
    public MylarMetadata Metadata { get; init; }
    
    public class MylarMetadata
    {
        [JsonPropertyName("type")]
        public string Type { get; init; }
        [JsonPropertyName("publisher")]
        public string Publisher { get; init; }
        [JsonPropertyName("imprint")]
        public string? Imprint { get; init; }
        [JsonPropertyName("name")]
        public string Name { get; init; }
        [JsonPropertyName("comicid")]
        public int ComicId { get; init; }
        [JsonPropertyName("year")]
        public int Year { get; init; }
        [JsonPropertyName("description_text")]
        public string? Description { get; init; }
        [JsonPropertyName("description_formatted")]
        public string? DescriptionFormatted { get; init; }
        [JsonPropertyName("volume")]
        public int? Volume { get; init; }
        [JsonPropertyName("booktype")]
        public string BookType { get; init; } // one of Print, OneShot, TPB, or GN
        [JsonPropertyName("age_rating")]
        public string? AgeRating { get; init; }
        [JsonPropertyName("comic_image")]
        public string CoverImageUrl { get; init; }
        [JsonPropertyName("total_issues")]
        public int TotalIssues { get; init; }
        [JsonPropertyName("publication_run")]
        public string PublicationRun { get; init; } // in format Month Year - Month Year, if series is ongoing, the end value is `Present`
        [JsonPropertyName("status")]
        public string Status { get; init; } // one of `Continuing` or `Ended`
    }
    #pragma warning restore CS8618

    public static MetadataMylar FromComicInfo(ComicInfo comicInfo, Func<Uri> alternateCover)
    {
        return new MetadataMylar
        {
            Metadata = new MylarMetadata
            {
                Type = "comicSeries",
                AgeRating = comicInfo.AgeRating.GetMylarDescription(typeof(ComicInfo.AgeRatingType)),
                BookType = "Print",
                ComicId = comicInfo.Identifier,
                Year = comicInfo.Year,
                CoverImageUrl = (comicInfo.Covers?.FirstOrDefault().Item2.Location ?? alternateCover()).ToString(),
                TotalIssues = (int)MathF.Floor(comicInfo.TotalChapters),
                Description = comicInfo.Description?.ReplaceLineEndings(""),
                DescriptionFormatted = comicInfo.Description,
                Name = comicInfo.Title,
                Volume = comicInfo.TotalVolumes,
                Imprint = null,
                PublicationRun = $"{comicInfo.Year}",
                Status = comicInfo.Status.GetMylarDescription(typeof(ComicInfo.StatusType)),
                Publisher = string.Join(", ", comicInfo.Publishers),
            }
        };
    }
}

[JsonSerializable(typeof(MetadataMylar))]
internal partial class MylarJsonContext : JsonSerializerContext;

[AttributeUsage(AttributeTargets.All)]
sealed class MylarDescriptionAttribute(string description) : Attribute
{
    public string Description => DescriptionValue;
    private string DescriptionValue { get; } = description;

    public override bool Equals([NotNullWhen(true)] object? obj) =>
        obj is MylarDescriptionAttribute other && other.Description == Description;

    public override int GetHashCode() => Description.GetHashCode();
}
