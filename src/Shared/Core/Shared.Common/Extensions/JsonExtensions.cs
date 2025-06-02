// ----------------------------------------------------------------------------------------------
// <copyright file="JsonExtensions.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Shared.Common.Extensions;

/// <summary>
/// Расширения для элементов Json.
/// </summary>
public static class JsonExtensions
{
    /// <summary>
    /// Преобразует <see cref="JsonDocument"/> в <see cref="object"/> или <see langword="null"/>.
    /// </summary>
    /// <param name="element">Элемент для преобразования.</param>
    /// <returns>Преобразованный <see cref="object"/> или <see langword="null"/>.</returns>
    public static object? ToObject(this JsonDocument element) => element.RootElement.ToObject();

    /// <summary>
    /// Преобразует <see cref="JsonElement"/> в <see cref="object"/> или <see langword="null"/>.
    /// </summary>
    /// <param name="element">Элемент для преобразования.</param>
    /// <returns>Преобразованный <see cref="object"/> или <see langword="null"/>.</returns>
    public static object? ToObject(this JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.Object => new Dictionary<string, object?>(element
                .EnumerateObject()
                .Select(pair => new KeyValuePair<string, object?>(pair.Name, pair.Value.ToObject()))),
            JsonValueKind.Array => new List<object?>(element
                .EnumerateArray()
                .Select(item => item.ToObject())),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw new NotSupportedException("Unsupported JSON value " + element.ValueKind)
        };
    }
}
