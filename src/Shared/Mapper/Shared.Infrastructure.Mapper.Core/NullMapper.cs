// ----------------------------------------------------------------------------------------------
// <copyright file="NullMapper.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Mapping;
using Shared.Domain.Core.Mapping.Interfaces;
using Shared.Infrastructure.Mapper.Core.Scope;
using IMapper = Shared.Domain.Core.Mapping.Interfaces.IMapper;

namespace Shared.Infrastructure.Mapper.Core;

/// <summary>
/// Заглушка <see cref="IMapper"/>, используемая когда <see cref="ResolutionContext"/>
/// нужен, но <see cref="MapperContextAccessor"/> ещё не выставлен (например, при построении профилей в режиме без DI).
/// Все операции пробрасывают <see cref="NotSupportedException"/> с указанием типа источника и назначения,
/// чтобы место вызова было легко идентифицировать в стеке.
/// </summary>
internal sealed class NullMapper
    : IMapper
{
    /// <inheritdoc />
    public TResult Map<TSource, TResult>(TSource source) =>
        throw new NotSupportedException(BuildMessage<TSource, TResult>(nameof(Map)));

    /// <inheritdoc />
    public IQueryable<TResult> ProjectTo<TResult>(IQueryable source, object? parameters = null) =>
        throw new NotSupportedException(BuildMessage<object, TResult>(nameof(ProjectTo)));

    /// <inheritdoc />
    public void Map<TSource, TResult>(TSource source, TResult result) =>
        throw new NotSupportedException(BuildMessage<TSource, TResult>(nameof(this.Map)));

    private static string BuildMessage<TSource, TResult>(string operation)
    {
        return $"{operation}<{typeof(TSource).Name}, {typeof(TResult).Name}>: " +
            $"{nameof(IMapper)} is not available in this context. " +
            $"Likely causes: (a) mapping was invoked from a test that did not register {nameof(IMapper)} in DI; " +
            $"(b) a user-supplied {typeof(ITypeConverter<,>).Name} / custom converter was called outside the registered mapper scope. " +
            $"Register an {nameof(IMapper)} implementation via {typeof(DependencyInjection.DependencyInjectorBase<,>).Name} or call mapping through the injected mapper.";
    }
}
