// ----------------------------------------------------------------------------------------------
// <copyright file="CloneCommand.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Cqrs.Core.Abstractions.Commands.Requests;

/// <summary>
/// Базовая команда клонирования сущности.
/// </summary>
/// <typeparam name="TRequest">Тип DTO с дополнительными данными для клонирования.</typeparam>
/// <typeparam name="TResponse">Тип ответа обработчика.</typeparam>
/// <param name="Key">Ключ исходной сущности для клонирования.</param>
/// <param name="Request">DTO с дополнительными данными для клонирования.</param>
public abstract record CloneCommand<TRequest, TResponse>(object Key, TRequest Request)
    : ICommand<TResponse>;