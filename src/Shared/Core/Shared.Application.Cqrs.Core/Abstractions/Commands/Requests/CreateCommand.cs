// ----------------------------------------------------------------------------------------------
// <copyright file="CreateCommand.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Cqrs.Core.Abstractions.Commands.Requests;

/// <summary>
/// Базовая команда создания новой сущности.
/// </summary>
/// <typeparam name="TRequest">Тип DTO с данными для создания.</typeparam>
/// <typeparam name="TResponse">Тип ответа обработчика.</typeparam>
/// <param name="Request">DTO с данными для создания.</param>
public abstract record CreateCommand<TRequest, TResponse>(TRequest Request)
    : ICommand<TResponse>;
