// ----------------------------------------------------------------------------------------------
// <copyright file="IExceptionMapper.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dto.Responses;

namespace Shared.Presentation.Core.Exceptions.Interfaces;

/// <summary>
/// Не-generic контракт маппера для резолва по типу исключения (порядконезависимый выбор).
/// </summary>
/// <remarks>
/// Резолвер использует <see cref="HandledType"/> и иерархию типов: для исключения ищется маппер
/// по <see cref="Exception.GetType"/> и его базовым типам; выбирается самый производный тип.
/// Порядок регистрации мапперов в DI не влияет на результат.
/// </remarks>
public interface IExceptionMapper
{
    /// <summary>
    /// Тип исключения, который обрабатывает данный маппер.
    /// </summary>
    Type HandledType { get; }

    /// <summary>
    /// Преобразует исключение в коллекцию деталей ошибки для HTTP-ответа.
    /// </summary>
    /// <param name="exception">Исключение для маппинга.</param>
    /// <returns>Экземпляр <see cref="ErrorResponse"/>.</returns>
    ErrorResponse Map(Exception exception);
}

/// <summary>
/// Типобезопасный маппер исключения в детали HTTP-ответа (RFC 7807 Problem Details).
/// </summary>
/// <typeparam name="TException">Тип исключения, которое маппер умеет преобразовывать.</typeparam>
/// <remarks>
/// Реализации регистрируются в DI как <see cref="IExceptionMapper"/>; резолвер выбирает маппер
/// по иерархии типов (<see cref="IExceptionMapper.HandledType"/>), порядок регистрации не важен.
/// </remarks>
public interface IExceptionMapper<in TException>
    : IExceptionMapper
    where TException : Exception
{
    /// <summary>
    /// Преобразует исключение в коллекцию деталей ошибки для HTTP-ответа.
    /// </summary>
    /// <param name="exception">Исключение для маппинга.</param>
    /// <returns>Экземпляр <see cref="ErrorResponse"/>.</returns>
   ErrorResponse Handle(TException exception);
}
