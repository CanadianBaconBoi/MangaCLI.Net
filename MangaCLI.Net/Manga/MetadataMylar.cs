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
using System.Text.Json.Serialization;

namespace MangaCLI.Net.Manga;

#pragma warning disable CS8618
public class MetadataMylar
{
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
        public string? DescriptionHtml { get; init; }
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

    public static MetadataMylar FromComicInfo(ComicInfo comicInfo, Func<Uri> alternateCover)
    {
        return new MetadataMylar
        {
            Metadata = new MetadataMylar.MylarMetadata
            {
                Type = "comicSeries",
                AgeRating = comicInfo.AgeRating.GetMylarDescription(),
                BookType = "Print",
                ComicId = comicInfo.Identifier,
                Year = comicInfo.Year,
                CoverImageUrl = (comicInfo.Covers.FirstOrDefault().Value.Location ?? alternateCover()).ToString(),
                TotalIssues = (int)MathF.Floor(comicInfo.TotalChapters ?? 0),
                Description = comicInfo.Description?.ReplaceLineEndings(""),
                DescriptionHtml = comicInfo.DescriptionHtml,
                Name = comicInfo.Title,
                Volume = comicInfo.TotalVolumes,
                Imprint = null,
                PublicationRun = $"{comicInfo.Year}",
                Status = comicInfo.Status.GetMylarDescription(),
                Publisher = string.Join(", ", comicInfo.Publishers),
            }
        };
    }
}

[JsonSerializable(typeof(MetadataMylar))]
internal partial class MylarJsonContext : JsonSerializerContext;

partial class MylarDescriptionAttribute: DescriptionAttribute
{
    public MylarDescriptionAttribute(string description) => this.DescriptionValue = description;
}
#pragma warning restore CS8618
