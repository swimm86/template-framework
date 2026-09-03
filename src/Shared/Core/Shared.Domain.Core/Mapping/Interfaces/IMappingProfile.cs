// ----------------------------------------------------------------------------------------------
// <copyright file="IMappingProfile.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Mapping.Description.Interfaces;

namespace Shared.Domain.Core.Mapping.Interfaces;

/// <summary>
/// Описание профиля преобразования (mapping), провайдеро-независимое.
/// </summary>
internal interface IMappingProfile
{
    /// <summary>
    /// Возвращает все описания преобразований (mapping), зарегистрированные в профиле.
    /// </summary>
    /// <returns>Коллекция описаний преобразований, зарегистрированных в профиле.</returns>
    IReadOnlyCollection<IMapDescription> GetMapDescriptions();
}
