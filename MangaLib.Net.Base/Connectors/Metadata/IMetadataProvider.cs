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

using System.ComponentModel.DataAnnotations;
using MangaLib.Net.Base.Models;

namespace MangaLib.Net.Base.Connectors.Metadata;

public interface IMetadataProvider
{
    public static abstract IMetadataProvider GetInstanceForComic(string identifier);

    public string Identifier { get; }
    public Task<ComicInfo?> GetComicInfo();
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class MetadataProviderDescriptorAttribute(string name, [Range(0, int.MaxValue/2)] int defaultPriority) : Attribute
{
    public string Name => NameValue;
    private string NameValue { get; } = name;
    
    public int DefaultPriority => DefaultPriorityValue;
    
    private int DefaultPriorityValue { get; } = defaultPriority < int.MaxValue/2 ? defaultPriority : throw new ArgumentOutOfRangeException();
}
