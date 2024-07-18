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

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using MangaLib.Net.Base.Metadata;

namespace MangaLib.Net.Base.Helpers;

public static class UriExtensions
{
    public static Uri Combine(this Uri self, string other) => new(self, other);

    public static Uri Combine(this Uri self, Uri other) => new(self, other);

    public static Uri CombineRaw(this Uri self, string other) => new($"{self}{other}");

    public static Uri CombineRaw(this Uri self, Uri other) => new($"{self}{other}");
}

public static class EnumDescriptionExtension
{
    public static string GetDescription<T>(this T enumerationValue,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
        Type enumerationType)
        where T : Enum
    {
        if (enumerationValue.GetType() != enumerationType)
            throw new TypeAccessException("Type passed to GetDescription is not equal to type of value passed");
        return enumerationType
            .GetField(enumerationValue.ToString())
            ?.GetCustomAttributes(typeof(DescriptionAttribute), false)
            .FirstOrDefault() is DescriptionAttribute attribute
            ? attribute.Description
            : enumerationValue.ToString();
    }

    public static string GetMylarDescription<T>(this T enumerationValue,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
        Type enumerationType)
        where T : Enum
    {
        if (enumerationValue.GetType() != enumerationType)
            throw new TypeAccessException("Type passed to GetMylarDescription is not equal to type of value passed");
        return enumerationType
            .GetField(enumerationValue.ToString())
            ?.GetCustomAttributes(typeof(MylarDescriptionAttribute), false)
            .FirstOrDefault() is MylarDescriptionAttribute attribute
            ? attribute.Description
            : enumerationValue.ToString();
    }
}

public static class EnumerableExtensions
{
    public static IEnumerable<T> NullToEmpty<T>(this IEnumerable<T>? src) => src ?? [];
    public static T[]? EmptyToNull<T>(this T[] src) => src.Length == 0 ? null : src;
}
