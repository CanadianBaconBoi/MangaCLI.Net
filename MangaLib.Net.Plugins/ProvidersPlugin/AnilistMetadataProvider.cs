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
using MangaLib.Net.Base.Connectors.Metadata;
using MangaLib.Net.Base.Models;
using MangaLib.Net.Base.Helpers;
using SkiaSharp;

namespace Providers;

[MetadataProviderDescriptor("anilist", 100)]
public class AnilistMetadataProvider(string identifier) : IMetadataProvider
{
    private static readonly AniClient AnilistClient = new();

    private static Dictionary<string, AnilistMetadataProvider> _providers = new();
    public static IMetadataProvider GetInstanceForComic(string identifier)
        => _providers.TryGetValue(identifier, out var provider)
            ? provider
            : _providers[identifier] = new AnilistMetadataProvider(identifier);

    public string Identifier { get; } = identifier;

    private Media? _anilistInfo;
    private bool _infoAvailable = true;

    private async Task<Media?> GetAnilistInfo()
    {
        if (!_infoAvailable)
            return null;
        var failCounter = 0;
        while (true)
        {
            try
            {
                return _anilistInfo ??= await AnilistClient.GetMediaAsync(
                    int.Parse(Identifier)
                );
            }
            catch (Exception e)
            {
                if (failCounter++ < 3)
                {
                    await Task.Delay(5000);
                    await Console.Out.WriteLineAsync(e.ToString());
                    continue;
                }

                _infoAvailable = false;
                return null;
            }
        }
    }

    private AniPagination<StaffEdge>? _anilistStaff;
    private bool _staffAvailable = true;

    private async Task<AniPagination<StaffEdge>?> GetAnilistStaff()
    {
        if(!_staffAvailable)
            return null;
        var failCounter = 0;
        while (true)
        {
            try
            {
                return _anilistStaff ??= await (await GetAnilistInfo())?.GetStaffAsync()!;
            }
            catch
            {
                if (failCounter++ < 3)
                {
                    await Task.Delay(5000);
                    continue;
                }

                _staffAvailable = false;
                return null;
            }
        }
    }
    
    private StudioEdge[]? _anilistStudios;
    private bool _studiosAvailable = true;

    private async Task<StudioEdge[]?> GetAnilistStudios()
    {
        if(!_studiosAvailable)
            return null;
        var failCounter = 0;
        while (true)
        {
            try
            {
                return _anilistStudios ??= await (await GetAnilistInfo())?.GetStudiosAsync()!;
            }
            catch
            {
                if (failCounter++ < 3)
                {
                    await Task.Delay(5000);
                    continue;
                }

                _studiosAvailable = false;
                return null;
            }
        }
    }
    
    private MediaTag[]? _anilistTags;
    private bool _tagsAvailable = true;

    private async Task<MediaTag[]?> GetAnilistTags()
    {
        if(!_tagsAvailable)
            return null;
        var failCounter = 0;
        while (true)
        {
            try
            {
                return _anilistTags ??= await (await GetAnilistInfo())?.GetTagsAsync()!;
            }
            catch
            {
                if (failCounter++ < 3)
                {
                    await Task.Delay(5000);
                    continue;
                }

                _tagsAvailable = false;
                return null;
            }
        }
    }

    private AniPagination<CharacterEdge>? _anilistCharacters;
    private bool _charactersAvailable = true;

    private async Task<AniPagination<CharacterEdge>?> GetAnilistCharacters()
    {
        if(!_charactersAvailable)
            return null;
        var failCounter = 0;
        while (true)
        {
            try
            {
                return _anilistCharacters ??= await (await GetAnilistInfo())?.GetCharactersAsync()!;
            }
            catch
            {
                if (failCounter++ < 3)
                {
                    await Task.Delay(5000);
                    continue;
                }

                _charactersAvailable = false;
                return null;
            }
        }
    }

    private AniPagination<MediaReview>? _anilistReviews;
    private bool _reviewsAvailable = true;

    private async Task<AniPagination<MediaReview>?> GetAnilistReviews()
    {
        if(!_reviewsAvailable)
            return null;
        var failCounter = 0;
        while (true)
        {
            try
            {
                return _anilistReviews ??= await (await GetAnilistInfo())?.GetReviewsAsync()!;
            }
            catch
            {
                if (failCounter++ < 3)
                {
                    await Task.Delay(5000);
                    continue;
                }
                _reviewsAvailable = false;
                return null;
            }
        }
    }
    
    public async Task<ComicInfo?> GetComicInfo()
    {
        const StringComparison strCmp = StringComparison.InvariantCultureIgnoreCase;
        return await GetAnilistInfo() is { } anilistInfo
            ? await GetAnilistStaff() is {} anilistStaff ? new ComicInfo
            {
                Identifier = anilistInfo.Id,
                
                Authors = anilistStaff.Data
                    .Where(edge =>
                        (edge.Role.Contains("write", strCmp) ||
                         edge.Role.Contains("story", strCmp) ||
                         edge.Role.Contains("author", strCmp))
                        && edge.Staff.Name.FullName != null
                    )
                    .Select(edge => edge.Staff.Name.FullName!).ToArray().EmptyToNull(),
                Penciller = anilistStaff.Data
                    .Where(edge => (edge.Role.Contains("pencil", strCmp) || edge.Role.Contains("art", strCmp)) && edge.Staff.Name.FullName != null)
                    .Select(edge => edge.Staff.Name.FullName!).ToArray().EmptyToNull(),
                Inker = anilistStaff.Data
                    .Where(edge => (edge.Role.Contains("ink", strCmp) || edge.Role.Contains("black", strCmp)) && edge.Staff.Name.FullName != null)
                    .Select(edge => edge.Staff.Name.FullName!).ToArray().EmptyToNull(),
                Colorist = anilistStaff.Data
                    .Where(edge => (edge.Role.Contains("color", strCmp) || edge.Role.Contains("colour", strCmp)) && edge.Staff.Name.FullName != null)
                    .Select(edge => edge.Staff.Name.FullName!).ToArray().EmptyToNull(),
                Letterer = anilistStaff.Data
                    .Where(edge => edge.Role.Contains("letter", strCmp) && edge.Staff.Name.FullName != null)
                    .Select(edge => edge.Staff.Name.FullName!).ToArray().EmptyToNull(),
                CoverArtist = anilistStaff.Data
                    .Where(edge => edge.Role.Contains("cover", strCmp) && edge.Staff.Name.FullName != null)
                    .Select(edge => edge.Staff.Name.FullName!).ToArray().EmptyToNull(),
                Editors = anilistStaff.Data
                    .Where(edge => edge.Role.Contains("edit", strCmp) && edge.Staff.Name.FullName != null)
                    .Select(edge => edge.Staff.Name.FullName!).ToArray().EmptyToNull(),
                Publishers = (await GetAnilistStudios())?.Select(studio => studio.Studio.Name).ToArray().EmptyToNull(),

                Title = anilistInfo.Title.EnglishTitle ?? anilistInfo.Title.RomajiTitle,
                Country = null, //TODO: Find AniList Library with this info that doesn't suck
                Status = anilistInfo.Status switch
                {
                    MediaStatus.Releasing => ComicInfo.StatusType.Continuing,
                    MediaStatus.Hiatus => ComicInfo.StatusType.Continuing,
                    MediaStatus.Canceled => ComicInfo.StatusType.Ended,
                    MediaStatus.Finished => ComicInfo.StatusType.Ended,
                    null => null,
                    _ => ComicInfo.StatusType.Unknown
                },
                Links = new Dictionary<string, string?>
                    {
                        { "al", $"https://anilist.co/manga/{anilistInfo.Id}" },
                        { "mal", $"https://myanimelist.net/manga/{anilistInfo.MalId}" }
                    }
                    .Where(pair => pair.Value != null).ToDictionary()!,
                TotalChapters = anilistInfo.Chapters,
                TotalVolumes = anilistInfo.Volumes,
                Description = anilistInfo.Description,
                DescriptionHtml = anilistInfo.DescriptionHtml,
                StartDate = anilistInfo.StartDate.Year is { }
                    ? new DateOnly(
                        anilistInfo.StartDate.Year.Value,
                        anilistInfo.StartDate.Month ?? 1,
                        anilistInfo.StartDate.Day ?? 1
                    )
                    : null,
                EndDate = anilistInfo.EndDate.Year is { }
                    ? new DateOnly(
                        anilistInfo.EndDate.Year.Value,
                        anilistInfo.EndDate.Month ?? 1,
                        anilistInfo.EndDate.Day ?? 1
                    )
                    : null,
                CommunityRating = anilistInfo.AverageScore / 20,
                AgeRating = anilistInfo.IsAdult ? ComicInfo.AgeRatingType.M : ComicInfo.AgeRatingType.Everyone,
                AlternateTitles = (new Dictionary<string, string?>
                {
                    { "en", anilistInfo.Title.EnglishTitle },
                    { "ja", anilistInfo.Title.NativeTitle },
                    { "ja-ro", anilistInfo.Title.RomajiTitle }
                }.Where(pair => pair.Value != null).ToDictionary() as Dictionary<string, string>) is { } titleDict
                    ? (titleDict.Count == 0 ? null : titleDict)
                    : null,
                Genres = anilistInfo.Genres,
                Tags = (await GetAnilistTags())?.Select(tag => tag.Name).ToArray().EmptyToNull(),
                Covers = GetAnilistCover(anilistInfo).ToArray().EmptyToNull(),

                Review = (await GetAnilistReviews())?.Data.FirstOrDefault()?.Summary,

                Characters = (await GetAnilistCharacters())?.Data
                    .Where(character => character.Role == CharacterRole.Main)
                    .Select(character =>
                        character.Character.Name.FullName ?? character.Character.Name.UserPreferred)
                    .ToArray().EmptyToNull()
            } : new ComicInfo
            {
                Identifier = anilistInfo.Id,
                Publishers = (await GetAnilistStudios())?.Select(studio => studio.Studio.Name).ToArray().EmptyToNull(),

                Title = anilistInfo.Title.EnglishTitle ?? anilistInfo.Title.RomajiTitle,
                Country = null, //TODO: Find AniList Library with this info that doesn't suck
                Status = anilistInfo.Status switch
                {
                    MediaStatus.Releasing => ComicInfo.StatusType.Continuing,
                    MediaStatus.Hiatus => ComicInfo.StatusType.Continuing,
                    MediaStatus.Canceled => ComicInfo.StatusType.Ended,
                    MediaStatus.Finished => ComicInfo.StatusType.Ended,
                    null => null,
                    _ => ComicInfo.StatusType.Unknown
                },
                Links = new Dictionary<string, string?>
                    {
                        { "al", $"https://anilist.co/manga/{anilistInfo.Id}" },
                        { "mal", $"https://myanimelist.net/manga/{anilistInfo.MalId}" }
                    }
                    .Where(pair => pair.Value != null).ToDictionary()!,
                TotalChapters = anilistInfo.Chapters,
                TotalVolumes = anilistInfo.Volumes,
                Description = anilistInfo.Description,
                DescriptionHtml = anilistInfo.DescriptionHtml,
                StartDate = anilistInfo.StartDate.Year is { }
                    ? new DateOnly(
                        anilistInfo.StartDate.Year.Value,
                        anilistInfo.StartDate.Month ?? 1,
                        anilistInfo.StartDate.Day ?? 1
                    )
                    : null,
                EndDate = anilistInfo.EndDate.Year is { }
                    ? new DateOnly(
                        anilistInfo.EndDate.Year.Value,
                        anilistInfo.EndDate.Month ?? 1,
                        anilistInfo.EndDate.Day ?? 1
                    )
                    : null,
                CommunityRating = anilistInfo.AverageScore / 20,
                AgeRating = anilistInfo.IsAdult ? ComicInfo.AgeRatingType.M : ComicInfo.AgeRatingType.Everyone,
                AlternateTitles = (new Dictionary<string, string?>
                {
                    { "en", anilistInfo.Title.EnglishTitle },
                    { "ja", anilistInfo.Title.NativeTitle },
                    { "ja-ro", anilistInfo.Title.RomajiTitle }
                }.Where(pair => pair.Value != null).ToDictionary() as Dictionary<string, string>) is { } titleDict1
                    ? (titleDict1.Count == 0 ? null : titleDict1)
                    : null,
                Genres = anilistInfo.Genres,
                Tags = (await GetAnilistTags())?.Select(tag => tag.Name).ToArray().EmptyToNull(),
                Covers = GetAnilistCover(anilistInfo).ToArray().EmptyToNull(),

                Review = (await GetAnilistReviews())?.Data.FirstOrDefault()?.Summary,

                Characters = (await GetAnilistCharacters())?.Data
                    .Where(character => character.Role == CharacterRole.Main)
                    .Select(character =>
                        character.Character.Name.FullName ?? character.Character.Name.UserPreferred)
                    .ToArray().EmptyToNull()
            }
            : null;
    }

    private IEnumerable<(string, ComicInfo.ImageType)> GetAnilistCover(Media? anilistInfo)
    {
        if (anilistInfo == null || string.IsNullOrEmpty(anilistInfo.Cover.ExtraLargeImageUrl.ToString()))
            yield break;

        using var response = MangaLib.Net.Base.MangaLib.GetClient()
            .Send(new HttpRequestMessage(HttpMethod.Get, anilistInfo.Cover.ExtraLargeImageUrl));
        using var image = SKImage.FromEncodedData(response.Content.ReadAsStream());
        yield return ("0", new ComicInfo.ImageType()
        {
            Width = image.Width,
            Height = image.Height,
            Location = anilistInfo.Cover.ExtraLargeImageUrl
        });
    }
}