// ----------------------------------------------------------------------------------------------
// <copyright file="IDescriptionApplicator.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Mapping.Description.Interfaces;

namespace Shared.Infrastructure.Mapper.Core.ProfileBuilder.Interfaces;

/// <summary>
/// Базовый контракт applicator-а: применяет соответствующую секцию
/// <see cref="IMapDescription"/> к провайдеро-специфичному контексту преобразования (mapping).
/// </summary>
internal interface IDescriptionApplicator
{
    /// <summary>
    /// Применить соответствующую секцию <paramref name="description"/> к провайдеро-специфичному
    /// контексту <paramref name="mappingContext"/>.
    /// </summary>
    /// <param name="mappingContext">Объект, к которому применяется настройка преобразования (mapping).</param>
    /// <param name="description">Описание преобразования.</param>
    void Apply(object mappingContext, IMapDescription description);
}
