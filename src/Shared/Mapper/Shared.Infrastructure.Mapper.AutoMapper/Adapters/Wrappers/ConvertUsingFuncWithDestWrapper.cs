// ----------------------------------------------------------------------------------------------
// <copyright file="ConvertUsingFuncWithDestWrapper.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Mapping;
using Shared.Domain.Core.Mapping.Interfaces;
using Shared.Infrastructure.Mapper.Core.Scope;

namespace Shared.Infrastructure.Mapper.AutoMapper.Adapters.Wrappers;

/// <summary>
/// Обёртка функции-конвертера (type converter) (src, dest, ctx) для AutoMapper <c>ConvertUsing</c>.
/// </summary>
/// <typeparam name="TSource">Исходный тип.</typeparam>
/// <typeparam name="TDestination">Целевой тип.</typeparam>
internal sealed class ConvertUsingFuncWithDestWrapper<TSource, TDestination>(
    Func<object, object, ResolutionContext, object> func)
{
    /// <summary>
    /// Выполняет конвертацию с поддержкой существующего целевого экземпляра
    /// при применении <c>ConvertUsing</c> в AutoMapper.
    /// </summary>
    /// <param name="source">Исходный объект.</param>
    /// <param name="dest">Существующий целевой объект для in-place обновления.</param>
    /// <param name="ctx">Контекст резолвера <see cref="global::AutoMapper.ResolutionContext"/>, передаваемый AutoMapper.</param>
    /// <returns>Результат конвертации.</returns>
    /// <exception cref="InvalidOperationException">Выбрасывается, если текущий <see cref="IMapper"/> недоступен через <see cref="MapperContextAccessor.Current"/>.</exception>
    public TDestination Invoke(TSource source, TDestination dest, global::AutoMapper.ResolutionContext ctx)
    {
        var mapper = MapperContextAccessor.Current
            ?? throw new InvalidOperationException(
                $"Current {nameof(IMapper)} is not available.");
        var sharedCtx = new ResolutionContext(mapper);
        return (TDestination)func(source!, dest!, sharedCtx);
    }
}
