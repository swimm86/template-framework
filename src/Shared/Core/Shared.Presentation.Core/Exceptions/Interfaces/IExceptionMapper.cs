// ----------------------------------------------------------------------------------------------
// <copyright file="IExceptionMapper.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dto.Responses;

namespace Shared.Presentation.Core.Exceptions.Interfaces;

/// <summary>
/// Не-generic контракт преобразователя исключений для выбора по типу исключения (независимо от порядка регистрации).
/// </summary>
/// <remarks>
/// Резолвер использует <see cref="HandledType"/> и иерархию типов: для исключения ищется преобразователь
/// по <see cref="Exception.GetType"/> и его базовым типам; выбирается самый производный тип.
/// Порядок регистрации преобразователей в DI не влияет на результат.
/// </remarks>
public interface IExceptionMapper
{
    /// <summary>
    /// Тип исключения, который обрабатывает данный преобразователь.
    /// </summary>
    Type HandledType { get; }

    /// <summary>
    /// Преобразует исключение в коллекцию деталей ошибки для HTTP-ответа.
    /// </summary>
    /// <param name="exception">Исключение для преобразования.</param>
    /// <returns>Экземпляр <see cref="ErrorResponse"/>.</returns>
    ErrorResponse Map(Exception exception);
}

/// <summary>
/// Типобезопасный преобразователь исключения в детали HTTP-ответа (RFC 7807 Problem Details).
/// </summary>
/// <typeparam name="TException">Тип исключения, который преобразователь умеет обрабатывать.</typeparam>
/// <remarks>
/// Реализации регистрируются в DI как <see cref="IExceptionMapper"/>; резолвер выбирает преобразователь
/// по иерархии типов (<see cref="IExceptionMapper.HandledType"/>), порядок регистрации не важен.
/// </remarks>
public interface IExceptionMapper<in TException>
    : IExceptionMapper
    where TException : Exception
{
    /// <summary>
    /// Преобразует исключение в коллекцию деталей ошибки для HTTP-ответа.
    /// </summary>
    /// <param name="exception">Исключение для преобразования.</param>
    /// <returns>Экземпляр <see cref="ErrorResponse"/>.</returns>
   ErrorResponse Handle(TException exception);
}
