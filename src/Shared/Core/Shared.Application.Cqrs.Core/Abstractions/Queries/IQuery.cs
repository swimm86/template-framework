// ----------------------------------------------------------------------------------------------
// <copyright file="IQuery.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using MediatR;

namespace Shared.Application.Cqrs.Core.Abstractions.Queries;

/// <summary>
/// Интерфейс запроса на чтение
/// </summary>
/// <typeparam name="TResponse">Тип ответа.</typeparam>
public interface IQuery<TResponse> : IRequest<TResponse>
{
}
