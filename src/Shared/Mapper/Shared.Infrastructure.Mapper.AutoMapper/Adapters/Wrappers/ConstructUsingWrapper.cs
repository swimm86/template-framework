// ----------------------------------------------------------------------------------------------
// <copyright file="ConstructUsingWrapper.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Mapping;
using Shared.Domain.Core.Mapping.Interfaces;
using Shared.Infrastructure.Mapper.Core.Scope;

namespace Shared.Infrastructure.Mapper.AutoMapper.Adapters.Wrappers;

/// <summary>
/// Обёртка конструктора объекта для AutoMapper <c>ConstructUsing</c>.
/// </summary>
/// <typeparam name="TSource">Исходный тип.</typeparam>
/// <typeparam name="TDestination">Целевой тип.</typeparam>
internal sealed class ConstructUsingWrapper<TSource, TDestination>(
    Func<object, ResolutionContext, object> func)
{
    /// <summary>
    /// Конструирует целевой объект при применении <c>ConstructUsing</c> в AutoMapper.
    /// </summary>
    /// <param name="source">Исходный объект.</param>
    /// <param name="ctx">Контекст резолвера <see cref="global::AutoMapper.ResolutionContext"/>, передаваемый AutoMapper.</param>
    /// <returns>Сконструированный целевой объект.</returns>
    /// <exception cref="InvalidOperationException">Выбрасывается, если текущий <see cref="IMapper"/> недоступен через <see cref="MapperContextAccessor.Current"/>.</exception>
    public TDestination Invoke(TSource source, global::AutoMapper.ResolutionContext ctx)
    {
        var mapper = MapperContextAccessor.Current
            ?? throw new InvalidOperationException(
                $"Current {nameof(IMapper)} is not available.");
        var sharedCtx = new ResolutionContext(mapper);
        return (TDestination)func(source!, sharedCtx);
    }
}
