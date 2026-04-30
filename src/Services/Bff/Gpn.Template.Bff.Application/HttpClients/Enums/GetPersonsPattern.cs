// ----------------------------------------------------------------------------------------------
// <copyright file="GetPersonsPattern.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Gpn.Template.Bff.Application.HttpClients.Enums;

/// <summary>
/// Режим вызова Getter API для получения списка сущностей «Person» (ветвление HTTP-маршрута).
/// </summary>
/// <remarks>
/// Соответствует сегменту пути в Getter: <c>persons/services/list</c> или <c>persons/cqrs/list</c>.
/// </remarks>
public enum GetPersonsPattern
{
    /// <summary>
    /// Обработка через CQRS: <c>POST persons/cqrs/list</c> (MediatR, <c>PersonReadListQuery</c>).
    /// </summary>
    Cqrs,

    /// <summary>
    /// Обработка через слой приложения Getter: <c>POST persons/services/list</c> (без MediatR).
    /// </summary>
    Services,
}
