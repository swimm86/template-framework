// ----------------------------------------------------------------------------------------------
// <copyright file="UpdateCommand.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Cqrs.Core.Abstractions.Commands.Requests;

/// <summary>
/// Команда обновления
/// </summary>
/// <param name="Key">Ключ по которому будет производиться поиск.</param>
/// <param name="Request">ДТО на обновление.</param>
/// <typeparam name="TRequest">Тип ДТО.</typeparam>
/// <typeparam name="TResponse">Ответ хендлера.</typeparam>
public abstract record UpdateCommand<TRequest, TResponse>(object Key, TRequest Request) : ICommand<TResponse>;
