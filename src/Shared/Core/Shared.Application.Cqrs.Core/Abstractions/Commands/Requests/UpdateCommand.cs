// ----------------------------------------------------------------------------------------------
// <copyright file="UpdateCommand.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Cqrs.Core.Abstractions.Commands.Requests;

/// <summary>
/// Базовая команда обновления существующей сущности.
/// </summary>
/// <typeparam name="TRequest">Тип DTO с данными для обновления.</typeparam>
/// <typeparam name="TResponse">Тип ответа обработчика.</typeparam>
/// <param name="Key">Ключ для поиска обновляемой сущности.</param>
/// <param name="Request">DTO с данными для обновления.</param>
public abstract record UpdateCommand<TRequest, TResponse>(object Key, TRequest Request) : ICommand<TResponse>;
