// ----------------------------------------------------------------------------------------------
// <copyright file="IExceptionMapperResolver.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dto.Responses;

namespace Shared.Presentation.Core.Exceptions.Interfaces;

/// <summary>
/// Преобразователь исключений, выбирающий подходящий <see cref="IExceptionMapper"/> по иерархии типов (независимо от порядка регистрации).
/// </summary>
/// <remarks>
/// Выбирает преобразователь по типу исключения и его базовым типам; берётся самый производный
/// зарегистрированный тип. Порядок регистрации преобразователей в DI не важен.
/// </remarks>
public interface IExceptionMapperResolver
{
    /// <summary>
    /// Преобразует исключение в <see cref="ErrorResponse"/> с помощью
    /// подходящего преобразователя (по <see cref="Exception.GetType"/> и базовым типам).
    /// </summary>
    /// <param name="exception">Исключение.</param>
    /// <returns>Коллекция деталей ошибки.</returns>
    ErrorResponse Map(Exception exception);
}
