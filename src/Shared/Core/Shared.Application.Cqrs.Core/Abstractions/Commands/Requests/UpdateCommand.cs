// ----------------------------------------------------------------------------------------------
// <copyright file="UpdateCommand.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using MediatR;

namespace Shared.Application.Cqrs.Core.Abstractions.Commands.Requests;

/// <summary>
/// Команда обновления
/// </summary>
/// <param name="Key">Ключ по которому будет производиться поиск.</param>
/// <param name="Dto">ДТО на обновление.</param>
/// <typeparam name="TKey">Тип ключа.</typeparam>
/// <typeparam name="TUpdateDto">Тип ДТО.</typeparam>
/// <typeparam name="TResponse">Ответ хендлера.</typeparam>
public abstract record UpdateCommand<TKey, TUpdateDto, TResponse>(TKey Key, TUpdateDto Dto) : IRequest<TResponse>;
