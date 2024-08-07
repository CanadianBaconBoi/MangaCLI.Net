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
using System.Text.Json;
using System.Text.Json.Serialization;
using MangaLib.Net.Base.Helpers;
using MangaLib.Net.Base.Models;

namespace MangaLib.Net.Base.Metadata;

public class MetadataMylar
{
#pragma warning disable CS8618
    [JsonPropertyName("version")] public string Version = "1.0.2";
    [JsonPropertyName("metadata")] public MylarMetadata Metadata { get; init; }

    public class MylarMetadata
    {
        [JsonPropertyName("type")] public string Type { get; init; }
        [JsonPropertyName("publisher")] public string Publisher { get; init; }
        [JsonPropertyName("imprint")] public string? Imprint { get; init; }
        [JsonPropertyName("name")] public string Name { get; init; }
        [JsonPropertyName("comicid")] public long ComicId { get; init; }
        [JsonPropertyName("year")] public int Year { get; init; }
        [JsonPropertyName("description_text")] public string? Description { get; init; }

        [JsonPropertyName("description_formatted")]
        public string? DescriptionFormatted { get; init; }

        [JsonPropertyName("volume")] public int? Volume { get; init; }
        
        /// <summary>
        /// One of Print, OneShot, TPB, or GN
        /// </summary>
        [JsonPropertyName("booktype")] public string BookType { get; init; }
        [JsonPropertyName("age_rating")] public string? AgeRating { get; init; }
        [JsonPropertyName("comic_image")] public string CoverImageUrl { get; init; }
        [JsonPropertyName("total_issues")] public int TotalIssues { get; init; }

        /// <summary>
        /// In format Month Year - Month Year, if series is ongoing, the end value is `Present`
        /// </summary>
        [JsonPropertyName("publication_run")] public string PublicationRun { get; init; }


        [JsonPropertyName("status")] public string Status { get; init; }
    }
#pragma warning restore CS8618
    
    public static MetadataMylar FromComicInfo(ComicInfo comicInfo, Func<Uri> alternateCover) =>
        new()
        {
            Metadata = new MylarMetadata
            {
                Type = "comicSeries",
                AgeRating = comicInfo.AgeRating?.GetMylarDescription(typeof(ComicInfo.AgeRatingType)),
                BookType = "Print",
                ComicId = comicInfo.Identifier,
                Year = comicInfo.StartDate?.Year ?? DateTime.Now.Year,
                CoverImageUrl = (comicInfo.Covers?.FirstOrDefault().Item2.Location ?? alternateCover()).ToString(),
                TotalIssues = (int)MathF.Floor(comicInfo.TotalChapters ?? 0),
                Description = comicInfo.Description?.ReplaceLineEndings(""),
                DescriptionFormatted = comicInfo.Description,
                Name = comicInfo.Title ?? string.Empty,
                Volume = comicInfo.TotalVolumes,
                Imprint = null,
                PublicationRun = $"{comicInfo.StartDate:MMMM yyyy} - {comicInfo.EndDate?.ToString("MMMM yyyy") ?? "Present"}",
                Status = comicInfo.Status?.GetMylarDescription(typeof(ComicInfo.StatusType))
                         ?? ComicInfo.StatusType.Unknown.GetMylarDescription(typeof(ComicInfo.StatusType)),
                Publisher = string.Join(", ", comicInfo.Publishers ?? [])
            }
        };

    public static void Serialize(Stream stream, MetadataMylar metadata)
    {
        JsonSerializer.Serialize(stream, metadata, MylarJsonContext.Default.MetadataMylar);
    }
    
    public static async Task SerializeAsync(Stream stream, MetadataMylar metadata)
    {
        await JsonSerializer.SerializeAsync(stream, metadata, MylarJsonContext.Default.MetadataMylar);
    }
}

[AttributeUsage(AttributeTargets.All)]
public sealed class MylarDescriptionAttribute(string description) : Attribute
{
    public string Description => DescriptionValue;
    private string DescriptionValue { get; } = description;

    public override bool Equals([NotNullWhen(true)] object? obj) =>
        obj is MylarDescriptionAttribute other && other.Description == Description;

    public override int GetHashCode() => Description.GetHashCode();
}

[JsonSerializable(typeof(MetadataMylar))]
internal partial class MylarJsonContext : JsonSerializerContext;
