// ----------------------------------------------------------------------------------------------
// <copyright file="Mapper.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Mapping.Interfaces;
using Shared.Infrastructure.Mapper.Core.Scope;

namespace Shared.Infrastructure.Mapper.AutoMapper;

/// <summary>
/// Преобразователь (mapper) на основе AutoMapper.
/// </summary>
internal sealed class Mapper
    : IMapper
{
    private readonly global::AutoMapper.IMapper _inner;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="Mapper"/>.
    /// </summary>
    /// <param name="inner">Внутренний AutoMapper-преобразователь (mapper).</param>
    public Mapper(global::AutoMapper.IMapper inner)
    {
        _inner = inner;
    }

    /// <inheritdoc />
    public TDestination Map<TSource, TDestination>(TSource source)
    {
        using var scope = new MapperContextScope(this);
        return _inner.Map<TSource, TDestination>(source);
    }

    /// <inheritdoc />
    public IQueryable<TDestination> ProjectTo<TDestination>(IQueryable source, object? parameters = null)
    {
        using var scope = new MapperContextScope(this);
        return _inner.ProjectTo<TDestination>(source, parameters);
    }

    /// <inheritdoc />
    public void Map<TSource, TResult>(TSource source, TResult result)
    {
        using var scope = new MapperContextScope(this);
        _inner.Map(source, result);
    }
}
