// ----------------------------------------------------------------------------------------------
// <copyright file="ParameterKeyRegistry.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using Shared.Infrastructure.Mapper.Mapster.Adapters;

namespace Shared.Infrastructure.Mapper.Mapster.Utils;

/// <summary>
/// Реестр ключей параметров, используемых в параметризованных MapFrom-выражениях.
/// Заполняется при конфигурации маппинга (в <see cref="MapsterProfileBuilder"/>),
/// используется во время <see cref="Mapper.ProjectTo{TResult}"/> для вызова AddParameters.
/// </summary>
internal static class ParameterKeyRegistry
{
    private static readonly ConcurrentDictionary<(Type Source, Type Dest), HashSet<string>> Keys = [];

    /// <summary>
    /// Зарегистрировать набор ключей параметров для пары типов.
    /// </summary>
    /// <param name="source">Исходный тип.</param>
    /// <param name="dest">Целевой тип.</param>
    /// <param name="keys">Набор ключей.</param>
    public static void Register(Type source, Type dest, HashSet<string> keys) =>
        Keys[(source, dest)] = keys;

    /// <summary>
    /// Возвращает зарегистрированные ключи параметров для пары типов или <c>null</c>, если для пары нет регистрации.
    /// </summary>
    /// <param name="source">Исходный тип.</param>
    /// <param name="dest">Целевой тип.</param>
    /// <returns>Набор ключей или <c>null</c>, если для пары нет регистрации.</returns>
    public static HashSet<string>? GetKeys(Type source, Type dest) =>
        Keys.TryGetValue((source, dest), out var keys) ? keys : null;
}
