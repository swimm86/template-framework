// ----------------------------------------------------------------------------------------------
// <copyright file="IExceptionMapperResolver.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dto.Responses;

namespace Shared.Presentation.Core.Exceptions.Interfaces;

/// <summary>
/// Резолвер маппера исключения по иерархии типов (порядконезависимый).
/// </summary>
/// <remarks>
/// Выбирает маппер по типу исключения и его базовым типам; берётся самый производный
/// зарегистрированный тип. Порядок регистрации мапперов в DI не важен.
/// </remarks>
public interface IExceptionMapperResolver
{
    /// <summary>
    /// Преобразует исключение в <see cref="ErrorResponse"/> с помощью
    /// подходящего маппера (по <see cref="Exception.GetType"/> и базовым типам).
    /// </summary>
    /// <param name="exception">Исключение.</param>
    /// <returns>Коллекция деталей ошибки.</returns>
    ErrorResponse Map(Exception exception);
}
