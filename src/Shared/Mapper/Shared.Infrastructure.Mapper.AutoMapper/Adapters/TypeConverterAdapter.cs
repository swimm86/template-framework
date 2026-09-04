// ----------------------------------------------------------------------------------------------
// <copyright file="TypeConverterAdapter.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Mapping;
using Shared.Domain.Core.Mapping.Interfaces;
using Shared.Infrastructure.Mapper.Core.Scope;

namespace Shared.Infrastructure.Mapper.AutoMapper.Adapters;

/// <summary>
/// Адаптер нашего <see cref="ITypeConverter{TSource, TDestination}"/> к AutoMapper-овскому <c>ITypeConverter</c>.
/// Используется, когда AutoMapper вызывает наш конвертер типов (type converter) в процессе преобразования (mapping).
/// </summary>
/// <typeparam name="TSource">Исходный тип.</typeparam>
/// <typeparam name="TDestination">Целевой тип.</typeparam>
/// <param name="inner">Внутренний провайдеро-независимый конвертер типов (type converter).</param>
internal sealed class TypeConverterAdapter<TSource, TDestination>(
    ITypeConverter<TSource, TDestination> inner)
    : global::AutoMapper.ITypeConverter<TSource, TDestination>
{
    /// <inheritdoc />
    public TDestination Convert(
        TSource source,
        TDestination destination,
        global::AutoMapper.ResolutionContext context)
    {
        var mapper = MapperContextAccessor.Current
            ?? throw new InvalidOperationException(
                $"Current {nameof(IMapper)} is not available. " +
                $"Ensure mapping is invoked through the registered {nameof(IMapper)} implementation.");
        var sharedContext = new ResolutionContext(mapper);
        return inner.Convert(source, destination, sharedContext);
    }
}
