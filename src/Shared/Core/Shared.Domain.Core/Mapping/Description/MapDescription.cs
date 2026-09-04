// ----------------------------------------------------------------------------------------------
// <copyright file="MapDescription.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Mapping.Description.Interfaces;

namespace Shared.Domain.Core.Mapping.Description;

/// <summary>
/// Внутренняя реализация <see cref="IMapDescription"/>.
/// </summary>
/// <typeparam name="TSource">Исходный тип.</typeparam>
/// <typeparam name="TDestination">Целевой тип.</typeparam>
internal sealed class MapDescription<TSource, TDestination>
    : IMapDescription
{
    #region Поля

    private readonly List<IMemberMapDescription> _members = [];
    private readonly List<ICtorParamMapDescription> _ctorParams = [];

    #endregion

    /// <inheritdoc />
    public Type SourceType => typeof(TSource);

    /// <inheritdoc />
    public Type DestinationType => typeof(TDestination);

    /// <inheritdoc />
    public IReadOnlyCollection<IMemberMapDescription> Members => _members;

    /// <inheritdoc />
    public IReadOnlyCollection<ICtorParamMapDescription> CtorParams => _ctorParams;

    /// <inheritdoc />
    public Action<object, object>? AfterMap { get; set; }

    /// <inheritdoc />
    public Action<object, object>? BeforeMap { get; set; }

    /// <inheritdoc />
    public Func<object, ResolutionContext, object>? ConstructUsing { get; set; }

    /// <inheritdoc />
    public object? Converter { get; set; }

    /// <inheritdoc />
    public Func<object, object>? ConvertUsingFunction { get; set; }

    /// <inheritdoc />
    public Func<object, object, ResolutionContext, object>? ConvertUsingFunctionWithDest { get; set; }

    /// <inheritdoc />
    public bool HasReverseMap { get; set; }

    /// <summary>
    /// Внутренний список базовых преобразований (mapping) для <c>IncludeBase</c>.
    /// </summary>
    public List<(Type SourceBase, Type DestBase)> IncludedBasesList { get; } = new();

    /// <inheritdoc />
    public IReadOnlyList<(Type SourceBase, Type DestBase)> IncludedBases => IncludedBasesList;

    /// <summary>
    /// Добавляет элемент в <see cref="CtorParams"/>.
    /// </summary>
    /// <param name="ctorParam">Описание настройки для преобразования (mapping) параметра конструктора.</param>
    internal void AddCtorParam(ICtorParamMapDescription ctorParam) =>
        _ctorParams.Add(ctorParam);

    /// <summary>
    /// Добавляет элемент в <see cref="Members"/>.
    /// </summary>
    /// <param name="member">Описание настройки для члена преобразования (mapping).</param>
    internal void AddMember(IMemberMapDescription member) =>
        _members.Add(member);
}
