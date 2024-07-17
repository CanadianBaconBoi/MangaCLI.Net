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

using MangaCLI.Net.Models;

namespace MangaCLI.Net.Connectors.Manga;

public interface IConnector
{
    static abstract IConnector GetInstance();
    
    private static Dictionary<string, ConnectorInstanceDelegate>? _connectors;

    public static Dictionary<string, ConnectorInstanceDelegate> Connectors
    {
        get
        {
            var methodDefinition = typeof(IConnector).GetMethod(nameof(GetInstance))!;
            return _connectors ??= AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(asm => asm.GetTypes())
                .Where(
                    type =>
                        typeof(IConnector).IsAssignableFrom(type)
                        && type.GetCustomAttributes(typeof(ConnectorDescriptorAttribute), false).Length != 0
                )
                .Select(
                    type =>
                        (type.GetCustomAttributes(typeof(ConnectorDescriptorAttribute), false).FirstOrDefault() is ConnectorDescriptorAttribute descriptor ? descriptor.Name: throw new NotImplementedException($"Improperly implemented metadata provider: {type.Name} : Missing Descriptor"), 
                            type.GetInterfaceMap(typeof(IConnector)).TargetMethods.First(method => method.Name.EndsWith(methodDefinition.Name)).CreateDelegate<ConnectorInstanceDelegate>()
                        )
                )
                .ToDictionary(StringComparer.InvariantCultureIgnoreCase);
        }
    }

    internal HttpClient GetClient();
    public IAsyncEnumerable<IComic?> SearchComics(string searchQuery);
    
    public delegate IConnector ConnectorInstanceDelegate();
}

[AttributeUsage(AttributeTargets.Class)]
internal sealed class ConnectorDescriptorAttribute(string name) : Attribute
{
    public string Name => NameValue;
    private string NameValue { get; } = name;
}