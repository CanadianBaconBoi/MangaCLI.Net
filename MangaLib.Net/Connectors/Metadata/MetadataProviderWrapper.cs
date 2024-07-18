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

using System.Reflection;
using MangaLib.Net.Base.Connectors.Metadata;
using MangaLib.Net.Base.Models;

namespace MangaLib.Net.Connectors.Metadata;

public class MetadataProviderWrapper(IMetadataProvider provider)
{
    private IMetadataProvider _provider = provider;
    
    public static void AddExternalProvider(Assembly assembly, Type type)
    {
        if (ExternalProviders.TryGetValue(assembly, out var list))
            list.Add(type);
        else
            ExternalProviders[assembly] = [type];
    }

    private static Dictionary<Assembly, List<Type>>
        ExternalProviders
    {
        get;
    } = new();
    
    private static Dictionary<string, (MetadataInstanceDelegate Delegate, int DefaultPriority)>? _metadataProviders;

    internal static Dictionary<string, (MetadataInstanceDelegate Delegate, int DefaultPriority)> MetadataProviders
    {
        get
        {
            var methodDefinition = typeof(IMetadataProvider).GetMethod("GetInstanceForComic")!;
            return _metadataProviders ??= typeof(MetadataProviderWrapper).Assembly.GetTypes()
                .Concat(ExternalProviders.SelectMany(pair => pair.Value))
                .Where(
                    type =>
                        typeof(IMetadataProvider).IsAssignableFrom(type)
                        && type.GetCustomAttributes(typeof(MetadataProviderDescriptorAttribute), false).Length != 0
                )
                .Select(
                    type =>
                        new KeyValuePair<string, (MetadataInstanceDelegate Delegate, int DefaultPriority)>(type.GetCustomAttributes(typeof(MetadataProviderDescriptorAttribute), false).FirstOrDefault() is MetadataProviderDescriptorAttribute descriptor ? descriptor.Name: throw new NotImplementedException($"Improperly implemented metadata provider: {type.Name} : Missing Descriptor"), 
                            (type.GetInterfaceMap(typeof(IMetadataProvider)).TargetMethods.First(method => method.Name.EndsWith(methodDefinition.Name)).CreateDelegate<MetadataInstanceDelegate>(), descriptor.DefaultPriority)
                    )
                )
                .Reverse()
                .DistinctBy(pair => pair.Key, StringComparer.InvariantCultureIgnoreCase)
                .ToDictionary(StringComparer.InvariantCultureIgnoreCase);
        }
    }

    public Task<ComicInfo?> GetComicInfo() => _provider.GetComicInfo();
    
    internal delegate IMetadataProvider MetadataInstanceDelegate(string identifier);
}