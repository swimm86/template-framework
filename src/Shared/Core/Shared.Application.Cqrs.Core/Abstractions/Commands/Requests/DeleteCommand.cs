// ----------------------------------------------------------------------------------------------
// <copyright file="DeleteCommand.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Cqrs.Core.Abstractions.Commands.Requests;

/// <summary>
/// Команда на удаление
/// </summary>
/// <param name="Key">Ключ.</param>
/// <typeparam name="TKey">Тип ключа.</typeparam>
/// <typeparam name="TResponse">Тип ответа.</typeparam>
public abstract record DeleteCommand<TKey, TResponse>(TKey Key) : ICommand<TResponse>;
