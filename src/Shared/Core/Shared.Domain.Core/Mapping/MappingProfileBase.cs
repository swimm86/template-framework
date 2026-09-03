// ----------------------------------------------------------------------------------------------
// <copyright file="MappingProfileBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Mapping.Description;
using Shared.Domain.Core.Mapping.Description.Interfaces;
using Shared.Domain.Core.Mapping.Expression;
using Shared.Domain.Core.Mapping.Expression.Interfaces;
using Shared.Domain.Core.Mapping.Interfaces;

namespace Shared.Domain.Core.Mapping;

/// <summary>
/// Абстрактный базовый класс профиля преобразования (mapping), провайдеро-независимый.
/// </summary>
/// <remarks>
/// Наследники объявляют преобразования в конструкторе через <see cref="CreateMap{TSource, TDestination}"/>.
/// Собранные описания преобразований затем применяются конкретным провайдером
/// (AutoMapper, Mapster и т.п.).
/// </remarks>
public abstract class MappingProfileBase
    : IMappingProfile
{
    private readonly List<IMapDescription> _descriptions = [];

    /// <inheritdoc />
    IReadOnlyCollection<IMapDescription> IMappingProfile.GetMapDescriptions() => GetMapDescriptions();

    /// <summary>
    /// Получить все описания преобразований (mapping), зарегистрированные в профиле.
    /// </summary>
    /// <returns>Коллекция описаний преобразований, зарегистрированных в профиле.</returns>
    internal IReadOnlyCollection<IMapDescription> GetMapDescriptions() => _descriptions;

    /// <summary>
    /// Зарегистрировать готовое описание преобразования (mapping) (например, для обратного преобразования).
    /// </summary>
    /// <param name="description">Описание преобразования.</param>
    internal void RegisterDescription(IMapDescription description) => _descriptions.Add(description);

    /// <summary>
    /// Регистрирует преобразование (mapping) между <typeparamref name="TSource"/> и <typeparamref name="TDestination"/>.
    /// </summary>
    /// <typeparam name="TSource">Исходный тип.</typeparam>
    /// <typeparam name="TDestination">Целевой тип.</typeparam>
    /// <returns>Выражение для fluent-настройки преобразования.</returns>
    protected IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>()
    {
        var description = new MapDescription<TSource, TDestination>();
        _descriptions.Add(description);
        return new MappingExpression<TSource, TDestination>(description, this);
    }
}
