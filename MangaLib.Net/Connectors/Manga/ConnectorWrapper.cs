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
using System.Reflection;
using MangaLib.Net.Base.Connectors.Manga;
using MangaLib.Net.Base.Models;

namespace MangaLib.Net.Connectors.Manga;

public class ConnectorWrapper(IConnector connector)
{
    private IConnector _connector = connector;

    public static bool TryGetConnector(string name, [MaybeNullWhen(false)] out ConnectorWrapper connector)
    {
        connector = null;
        if (Connectors.TryGetValue(name, out var connectorDelegate))
            connector = new ConnectorWrapper(connectorDelegate.Invoke());
        return Connectors.ContainsKey(name);
    }

    public static void AddExternalConnector(Assembly assembly, Type type)
    {
        if (ExternalConnectors.TryGetValue(assembly, out var list))
            list.Add(type);
        else
            ExternalConnectors[assembly] = [type];
    } 
    
    private static Dictionary<Assembly, List<Type>>
        ExternalConnectors
    {
        get;
    } = new();
    
    private static Dictionary<string, ConnectorInstanceDelegate>? _connectors;

    private static Dictionary<string, ConnectorInstanceDelegate> Connectors
    {
        get
        {
            
            var methodDefinition = typeof(IConnector).GetMethod("GetInstance")!;
            return _connectors ??= typeof(ConnectorWrapper).Assembly.GetTypes()
                .Concat(ExternalConnectors.SelectMany(pair => pair.Value))
                .Where(
                    type =>
                        typeof(IConnector).IsAssignableFrom(type)
                        && type.GetCustomAttributes(typeof(ConnectorDescriptorAttribute), false).Length != 0
                )
                .Select(
                    type =>
                        new KeyValuePair<string, ConnectorInstanceDelegate>(type.GetCustomAttributes(typeof(ConnectorDescriptorAttribute), false).FirstOrDefault() is ConnectorDescriptorAttribute descriptor ? descriptor.Name: throw new NotImplementedException($"Improperly implemented metadata provider: {type.Name} : Missing Descriptor"), 
                            type.GetInterfaceMap(typeof(IConnector)).TargetMethods.First(method => method.Name.EndsWith(methodDefinition.Name)).CreateDelegate<ConnectorInstanceDelegate>()
                        )
                )
                .Reverse()
                .DistinctBy(pair => pair.Key, StringComparer.InvariantCultureIgnoreCase)
                .ToDictionary(StringComparer.InvariantCultureIgnoreCase);
        }
    }
    
    public IAsyncEnumerable<IComic?> SearchComics(string searchQuery) => connector.SearchComics(searchQuery);

    private delegate IConnector ConnectorInstanceDelegate();
}