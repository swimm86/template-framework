// ----------------------------------------------------------------------------------------------
// <copyright file="FakeMapper.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Mapping.Interfaces;

namespace Shared.Testing.Doubles.Mapping;

/// <summary>
/// Тестовая реализация <see cref="IMapper"/>, выполняющая зарегистрированные делегаты
/// и считающая вызовы <c>Map</c>/<c>ProjectTo</c>/<c>Map(src, dest)</c>.
/// </summary>
public sealed class FakeMapper : IMapper
{
    private readonly Dictionary<(Type Src, Type Dst), object> _mappers = new();

    /// <summary>
    /// Число вызовов <see cref="Map{TSource, TResult}(TSource)"/>.
    /// </summary>
    public int MapCallCount { get; private set; }

    /// <summary>
    /// Число вызовов <see cref="ProjectTo{TResult}(IQueryable, object?)"/>.
    /// </summary>
    public int ProjectToCallCount { get; private set; }

    /// <summary>
    /// Число вызовов <see cref="Map{TSource, TResult}(TSource, TResult)"/>.
    /// </summary>
    public int MapInPlaceCallCount { get; private set; }

    /// <summary>
    /// Зарегистрировать делегат маппинга для пары <typeparamref name="TSource"/> → <typeparamref name="TResult"/>.
    /// </summary>
    /// <typeparam name="TSource">Исходный тип.</typeparam>
    /// <typeparam name="TResult">Целевой тип.</typeparam>
    /// <param name="mapper">Делегат маппинга.</param>
    public void RegisterMap<TSource, TResult>(Func<TSource, TResult> mapper)
        => _mappers[(typeof(TSource), typeof(TResult))] = mapper;

    /// <inheritdoc />
    public TResult Map<TSource, TResult>(TSource source)
    {
        MapCallCount++;
        if (_mappers.TryGetValue((typeof(TSource), typeof(TResult)), out var mapper))
        {
            return ((Func<TSource, TResult>)mapper)(source);
        }

        throw new InvalidOperationException($"No mapping: {typeof(TSource).Name} → {typeof(TResult).Name}");
    }

    /// <inheritdoc />
    public IQueryable<TResult> ProjectTo<TResult>(IQueryable source, object? parameters = null)
    {
        ProjectToCallCount++;
        return source.Cast<TResult>();
    }

    /// <inheritdoc />
    public void Map<TSource, TResult>(TSource source, TResult result)
    {
        MapInPlaceCallCount++;
    }
}
