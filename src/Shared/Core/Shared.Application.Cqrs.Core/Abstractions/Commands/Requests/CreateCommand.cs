// ----------------------------------------------------------------------------------------------
// <copyright file="CreateCommand.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Cqrs.Core.Abstractions.Commands.Requests;

/// <summary>
/// Создание.
/// </summary>
/// <param name="Dto">ДТО на создание.</param>
/// <typeparam name="TCreateDto">Тип ДТО.</typeparam>
/// <typeparam name="TResponse">Ответ из хендлера.</typeparam>
public abstract record CreateCommand<TCreateDto, TResponse>(TCreateDto Dto) : ICommand<TResponse>;
