// ----------------------------------------------------------------------------------------------
// <copyright file="ResolutionContext.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Mapping.Interfaces;

namespace Shared.Domain.Core.Mapping;

/// <summary>
/// Провайдеро-независимый контекст маппинга (mapping context).
/// <para>
/// Передаётся пользовательскому <see cref="ITypeConverter{TSource, TDestination}"/> через
/// параметр <c>context</c> и даёт доступ к вложенным вызовам
/// <see cref="IMapper.Map{TSource, TResult}(TSource)"/> без знания о провайдере.
/// </para>
/// </summary>
/// <param name="mapper">Преобразователь, доступный для вложенных вызовов из пользовательского конвертера.</param>
public class ResolutionContext(
    IMapper mapper)
{
    /// <summary>
    /// Преобразует произвольный источник в указанный целевой тип.
    /// </summary>
    /// <typeparam name="TDestination">Целевой тип.</typeparam>
    /// <param name="source">Исходный объект.</param>
    /// <returns>Результат преобразования (mapping).</returns>
    public TDestination Map<TDestination>(object source) =>
        mapper.Map<object, TDestination>(source);

    /// <summary>
    /// Преобразует источник типа <typeparamref name="TSource"/> в тип <typeparamref name="TDestination"/>.
    /// </summary>
    /// <typeparam name="TSource">Исходный тип.</typeparam>
    /// <typeparam name="TDestination">Целевой тип.</typeparam>
    /// <param name="source">Исходный объект.</param>
    /// <returns>Результат преобразования (mapping).</returns>
    public TDestination Map<TSource, TDestination>(TSource source) =>
        mapper.Map<TSource, TDestination>(source);

    /// <summary>
    /// Заполняет свойства существующего целевого объекта из источника.
    /// </summary>
    /// <typeparam name="TSource">Исходный тип.</typeparam>
    /// <typeparam name="TDestination">Целевой тип.</typeparam>
    /// <param name="source">Исходный объект.</param>
    /// <param name="destination">Целевой объект для заполнения.</param>
    public void Map<TSource, TDestination>(TSource source, TDestination destination) =>
        mapper.Map(source, destination);
}
