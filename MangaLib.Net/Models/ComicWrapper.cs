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

using MangaLib.Net.Base.Models;
using MangaLib.Net.Connectors.Metadata;

namespace MangaLib.Net.Models;

public class ComicWrapper(IComic comic): IComic
{
    private IComic _comic = comic;
    
    private static Dictionary<(Type, string), ComicInfoWrapper> _comicInfoCache = new();
    
    public async Task<ComicInfoWrapper> GetComicInfo(List<String> priorities, ComicInfoPriorities priorityOverrides)
    {
        if (_comicInfoCache.ContainsKey((GetType(), Identifier)))
            return _comicInfoCache[(GetType(), Identifier)];
        
        var comicInfos = new List<(string, ComicInfo RawComicInfo)> {("connector", RawComicInfo)};
        if (GetMetadataIdentifiers() == null) return _comicInfoCache[(GetType(), Identifier)] = ComicInfoWrapper.Merge(comicInfos, priorities, priorityOverrides);
        foreach (var (providerName, identifier) in GetMetadataIdentifiers()!)
        {
            if (!MetadataProviderWrapper.MetadataProviders.ContainsKey(providerName)) continue;
            var provider = MetadataProviderWrapper.MetadataProviders[providerName];
            var comicInfo = await provider.Delegate.Invoke(identifier).GetComicInfo();
            if (comicInfo == null) continue;
            comicInfos.Add((providerName, comicInfo));
        }
        
        return _comicInfoCache[(GetType(), Identifier)] = ComicInfoWrapper.Merge(comicInfos, priorities, priorityOverrides);
    }

    public string Title
    {
        get => _comic.Title;
        init => throw new NotImplementedException();
    }

    public string Identifier {
        get => _comic.Identifier;
        init => throw new NotImplementedException();
    }
    public string Slug {
        get => _comic.Slug;
        init => throw new NotImplementedException();
    }
    public string? Description {
        get => _comic.Description;
        init => throw new NotImplementedException();
    }
    public string? CoverThumbnail {
        get => _comic.CoverThumbnail;
        init => throw new NotImplementedException();
    }
    public string? CoverUrl {
        get => _comic.CoverUrl;
        init => throw new NotImplementedException();
    }
    public IAsyncEnumerable<IChapter> GetChapters(string language) => _comic.GetChapters(language);

    public Dictionary<string, string>? GetMetadataIdentifiers() => _comic.GetMetadataIdentifiers();
    public ComicInfo RawComicInfo => _comic.RawComicInfo;
}