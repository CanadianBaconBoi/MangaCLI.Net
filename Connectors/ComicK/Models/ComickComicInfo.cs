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

using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MangaLib.Net.Connectors.Manga.ComicK.Models;

public class ComickComicInfo
{
#pragma warning disable CS8618
    
    [JsonPropertyName("comic")] public ComicInfo Comic { get; init; }

    [JsonPropertyName("artists")] public Artist[]? Artists { get; init; }

    [JsonPropertyName("authors")] public Author[]? Authors { get; init; }

    public class ComicInfo
    {
        [JsonPropertyName("id")] public int Identifier { get; init; }
        [JsonPropertyName("title")] public string? Title { get; init; }
        [JsonPropertyName("country")] public string? Country { get; init; }
        [JsonPropertyName("status")] public int? Status { get; init; }
        [JsonPropertyName("links")] public Dictionary<string, string>? Links { get; init; }
        [JsonPropertyName("last_chapter")] public float? TotalChapters { get; init; }
        [JsonPropertyName("final_volume")] public string? FinalVolume { get; init; }
        [JsonPropertyName("desc")] public string? Description { get; init; }
        [JsonPropertyName("parsed")] public string? ParsedDecsription { get; init; }
        [JsonPropertyName("year")] public int? Year { get; init; }
        [JsonPropertyName("bayesian_rating")] public string? Rating { get; init; }
        [JsonPropertyName("content_rating")] public ComickContentRating? ContentRating { get; init; }
        [JsonPropertyName("md_titles")] public List<ComickTitle>? Titles { get; init; }

        [JsonPropertyName("md_comic_md_genres")]
        public List<ComickGenreWrapper>? Genres { get; init; }

        [JsonPropertyName("md_covers")] public List<ComickCover>? Covers { get; init; }
        [JsonPropertyName("mu_comics")] public ComickComics? ExtraComicInfo { get; init; }

        [JsonConverter(typeof(JsonStringEnumConverter<ComickContentRating>))]
        public enum ComickContentRating
        {
            [EnumMember(Value = "safe")] Safe,
            [EnumMember(Value = "suggestive")] Suggestive,
            [EnumMember(Value = "erotica")] Erotica
        }

        public class ComickTitle
        {
            [JsonPropertyName("title")] public string Title { get; init; }
            [JsonPropertyName("lang")] public string Language { get; init; }
        }

        public class ComickGenreWrapper
        {
            [JsonPropertyName("md_genres")] public ComickGenreType Genre { get; init; }

            public class ComickGenreType
            {
                [JsonPropertyName("name")] public string Name { get; init; }
                [JsonPropertyName("group")] public string Group { get; init; }
            }
        }


        public class ComickCover
        {
            [JsonPropertyName("vol")] public string? Volume { get; init; }
            [JsonPropertyName("w")] public int Width { get; init; }
            [JsonPropertyName("h")] public int Height { get; init; }
            [JsonPropertyName("b2key")] public string ImageKey { get; init; }
        }

        public class ComickComics
        {
            [JsonPropertyName("mu_comic_publishers")]
            public List<PublisherWrapper>? Publishers { get; init; }

            [JsonPropertyName("mu_comic_categories")]
            public List<ComicCategoryWrapper>? ComicCategories { get; init; }

            public class PublisherWrapper
            {
                [JsonPropertyName("mu_publishers")] public PublisherType Publisher { get; init; }

                public class PublisherType
                {
                    [JsonPropertyName("title")] public string Name { get; init; }
                }
            }

            public class ComicCategoryWrapper
            {
                [JsonPropertyName("mu_categories")] public ComicCategoryType ComicCategory { get; init; }
                [JsonPropertyName("positive_vote")] public int Upvotes { get; init; }
                [JsonPropertyName("negative_vote")] public int Downvotes { get; init; }

                public class ComicCategoryType
                {
                    [JsonPropertyName("title")] public string Name { get; init; }
                }
            }
        }
    }

    public class Artist
    {
        [JsonPropertyName("name")] public string Name { get; init; }
    }

    public class Author
    {
        [JsonPropertyName("name")] public string Name { get; init; }
    }
#pragma warning restore CS8618
}