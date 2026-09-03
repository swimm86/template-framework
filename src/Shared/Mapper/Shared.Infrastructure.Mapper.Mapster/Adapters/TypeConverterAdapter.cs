// ----------------------------------------------------------------------------------------------
// <copyright file="TypeConverterAdapter.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Mapping;
using Shared.Domain.Core.Mapping.Interfaces;
using Shared.Infrastructure.Mapper.Mapster.Utils;
using CoreNullMapper = Shared.Infrastructure.Mapper.Core.NullMapper;

namespace Shared.Infrastructure.Mapper.Mapster.Adapters;

/// <summary>
/// Адаптер, оборачивающий провайдеро-независимый <see cref="ITypeConverter{TSource, TDestination}"/>
/// в форму, пригодную для применения в Mapster-овском <c>MapWith</c> через лямбду.
/// Используется адаптером <see cref="MapsterProfileBuilder"/>.
/// </summary>
/// <typeparam name="TSource">Исходный тип.</typeparam>
/// <typeparam name="TDestination">Целевой тип.</typeparam>
internal sealed class TypeConverterAdapter<TSource, TDestination>(
    ITypeConverter<TSource, TDestination> inner)
{
    /// <summary>
    /// Вызывает обёрнутый конвертер типов (type converter). Если в <see cref="Shared.Infrastructure.Mapper.Mapster.Utils.MapperContextAccessor"/> сохранён
    /// существующий целевой объект (<see cref="Shared.Infrastructure.Mapper.Mapster.Utils.MapperContextAccessor.CurrentMappingTarget"/>),
    /// он передаётся в конвертер для in-place обновления; иначе создаётся пустой target,
    /// чтобы избежать подмены <c>List&lt;T&gt;</c> со стороны Mapster.
    /// Используется в Mapster <c>MapWith</c>.
    /// </summary>
    /// <param name="source">Исходный объект.</param>
    /// <returns>Результат конвертации.</returns>
    public TDestination Invoke(TSource source)
    {
        var mapper = Core.Scope.MapperContextAccessor.Current ?? new CoreNullMapper();
        var sharedContext = new ResolutionContext(mapper);

        var storedTarget = MapperContextAccessor.CurrentMappingTarget;
        var target = storedTarget is TDestination existing
            ? existing
            : default!;

        return inner.Convert(source, target, sharedContext);
    }
}
