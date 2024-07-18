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
using MetadataComicRack = MangaLib.Net.Base.Metadata.MetadataComicRack;

namespace MangaLib.Net.Models;

public class ChapterWrapper (IChapter chapter): IChapter
{
    private IChapter _chapter = chapter;

    public string? Title
    {
        get => _chapter.Title;
        set => _chapter.Title = value;
    }

    public string Identifier
    {
        get => _chapter.Identifier;
        init => throw new NotImplementedException();
    }

    public string? ChapterIndex
    {
        get => _chapter.ChapterIndex;
        init => throw new NotImplementedException();
    }

    public string? VolumeIndex
    {
        get => _chapter.VolumeIndex;
        set => _chapter.VolumeIndex = value;
    }

    public string[]? GroupName
    {
        get => _chapter.GroupName;
        set => _chapter.GroupName = value;
    }

    public IPage[] Pages => _chapter.Pages;

    public MetadataComicRack GetComicRackMetadata(Base.Models.ComicInfo comicInfo) =>
        _chapter.GetComicRackMetadata(comicInfo);
}