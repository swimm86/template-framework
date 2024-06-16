// ----------------------------------------------------------------------------------------------
// <copyright file="ICommand.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using MediatR;

namespace Shared.Application.Cqrs.Core.Abstractions.Commands;

/// <summary>
/// Интерфейс команды без ответа
/// </summary>
public interface ICommand : IRequest<Unit>
{
}

/// <summary>
/// Интерфейс команды с ответом
/// </summary>
/// <typeparam name="TResponse">Тип ответа.</typeparam>
public interface ICommand<TResponse> : IRequest<TResponse>
{
}
