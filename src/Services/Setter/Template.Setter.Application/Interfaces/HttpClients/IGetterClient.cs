// ----------------------------------------------------------------------------------------------
// <copyright file="IGetterClient.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dto.Responses;

namespace Template.Setter.Application.Interfaces.HttpClients;

/// <summary>
/// Тест.
/// </summary>
public interface IGetterClient
{
    /// <summary>
    /// Тест.
    /// </summary>
    /// <param name="cancellationToken">Тест.</param>
    /// <returns>Тест.</returns>
    Task<Response> TestExceptionChainAsync(CancellationToken cancellationToken = default);
}
