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

using MangaCLI.Net.Connectors.Manga.ComicK.Models;
using MangaCLI.Net.Connectors.Metadata;

namespace MangaCLI.Net.Models;

public interface IComic
{
    private static Dictionary<(Type, string), ComicInfo> _comicInfoCache = new();
    
    public string Title { get; init; }
    public string Identifier { get; init; }
    public string Slug { get; init; }
    public string? Description { get; init; }
    public string? CoverThumbnail { get; init; }
    public string? CoverUrl { get; init; }
    public IAsyncEnumerable<ComickChapter> GetChapters(string language);
    public Dictionary<string, string>? MetadataIdentifiers { get; }
    protected ComicInfo RawComicInfo { get; }
    
    public async Task<ComicInfo> GetComicInfo()
    {
        if (_comicInfoCache.ContainsKey((GetType(), Identifier)))
            return _comicInfoCache[(GetType(), Identifier)];
        
        var comicInfos = new List<(string Provider, ComicInfo Info)> {("connector", RawComicInfo)};
        if (MetadataIdentifiers == null) return _comicInfoCache[(GetType(), Identifier)] = ComicInfo.Merge(comicInfos, MangaCli.Config);
        foreach (var (providerName, identifier) in MetadataIdentifiers)
        {
            if (!IMetadataProvider.MetadataProviders.ContainsKey(providerName)) continue;
            var provider = IMetadataProvider.MetadataProviders[providerName];
            var comicInfo = await provider.Delegate.Invoke(identifier).GetComicInfo();
            if (comicInfo == null) continue;
            comicInfos.Add((providerName, comicInfo));
        }
        
        return _comicInfoCache[(GetType(), Identifier)] = ComicInfo.Merge(comicInfos, MangaCli.Config);
    }
}