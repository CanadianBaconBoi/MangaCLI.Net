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

namespace MangaLib.Net.Models;

public class ComicInfoPriorities
{
    [DataMember(Name = "identifier")]
    public List<String> Identifier { get; set; } = [];
    [DataMember(Name = "authors")]
    public List<String> Authors { get; set; } = [];
    [DataMember(Name = "artists")]
    public List<String> Artists { get; set; } = [];
    [DataMember(Name = "penciller")]
    public List<String> Penciller { get; set; } = [];
    [DataMember(Name = "inker")]
    public List<String> Inker { get; set; } = [];
    [DataMember(Name = "colorist")]
    public List<String> Colorist { get; set; } = [];
    [DataMember(Name = "letterer")]
    public List<String> Letterer { get; set; } = [];
    [DataMember(Name = "cover_artist")]
    public List<String> CoverArtist { get; set; } = [];
    [DataMember(Name = "editors")]
    public List<String> Editors { get; set; } = [];
    [DataMember(Name = "publishers")]
    public List<String> Publishers { get; set; } = [];
    [DataMember(Name = "title")]
    public List<String> Title { get; set; } = [];
    [DataMember(Name = "country")]
    public List<String> Country { get; set; } = [];
    [DataMember(Name = "status")]
    public List<String> Status { get; set; } = [];
    [DataMember(Name = "links")]
    public List<String> Links { get; set; } = [];
    [DataMember(Name = "total_chapters")]
    public List<String> TotalChapters { get; set; } = [];
    [DataMember(Name = "total_volumes")]
    public List<String> TotalVolumes { get; set; } = [];
    [DataMember(Name = "description")]
    public List<String> Description { get; set; } = [];
    [DataMember(Name = "description_html")]
    public List<String> DescriptionHtml { get; set; } = [];
    [DataMember(Name = "start_date")]
    public List<String> StartDate { get; set; } = [];
    [DataMember(Name = "end_date")]
    public List<String> EndDate { get; set; } = [];
    [DataMember(Name = "community_rating")]
    public List<String> CommunityRating { get; set; } = [];
    [DataMember(Name = "age_rating")]
    public List<String> AgeRating { get; set; } = [];
    [DataMember(Name = "alternate_titles")]
    public List<String> AlternateTitles { get; set; } = [];
    [DataMember(Name = "genres")]
    public List<String> Genres { get; set; } = [];
    [DataMember(Name = "tags")]
    public List<String> Tags { get; set; } = [];
    [DataMember(Name = "covers")]
    public List<String> Covers { get; set; } = [];
    [DataMember(Name = "review")]
    public List<String> Review { get; set; } = [];
    [DataMember(Name = "characters")]
    public List<String> Characters { get; set; } = [];
}