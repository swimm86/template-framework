// ----------------------------------------------------------------------------------------------
// <copyright file="JsonHelper.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Shared.Common.Helpers;

/// <summary>
/// Вспомогательный класс для работы с JSON (сериализация/десериализация через <see cref="JsonSerializer"/>).
/// </summary>
public static class JsonHelper
{
    /// <summary>
    /// Пытается десериализовать строку JSON в объект указанного типа.
    /// </summary>
    /// <typeparam name="T">Тип целевого объекта (должен быть ссылочным типом).</typeparam>
    /// <param name="json">Строка в формате JSON.</param>
    /// <param name="result">При успешной десериализации — результирующий объект; иначе — <c>null</c>.</param>
    /// <param name="options">Настройки сериализации <see cref="JsonSerializerOptions"/>.
    /// Если <c>null</c>, используются настройки по умолчанию.</param>
    /// <returns><c>true</c>, если десериализация прошла успешно; <c>false</c>, если не удалось десериализовать.</returns>
    public static bool TryDeserialize<T>(
        string json,
        out T? result,
        JsonSerializerOptions? options = null)
        where T : class
    {
        bool isParsed;
        result = null;
        try
        {
            result = JsonSerializer.Deserialize<T>(json, options);
            isParsed = true;
        }
        catch
        {
            isParsed = false;
        }

        return isParsed;
    }
}
