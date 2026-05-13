// ----------------------------------------------------------------------------------------------
// <copyright file="ApiClientDelegatingHandleMetadataAttribute.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.ApiClient.Handlers.Attributes.Base;

namespace Shared.Application.Core.ApiClient.Handlers.Attributes;

/// <summary>
/// Задает метаданные для delegating handler-ов.
/// </summary>
/// <param name="order">
/// Порядок регистрации обработчика в конвейере.
/// Меньшее значение означает более раннее выполнение.
/// </param>
/// <remarks>
/// Зарезервированные значения для <see cref="DelegatingHandler"/>:
/// 100 — установка корреляционного идентификатора.
/// </remarks>
/// <example>
/// <code>
/// [ApiClientHandlerMetadataAttribute(100, typeof(MyApiClient))]
/// public sealed class CorrelationIdDelegatingHandler : DelegatingHandler
/// {
/// }
/// </code>
/// </example>
/// <inheritdoc cref="ApiClientHandlerMetadataAttributeBase"/>
/// <param name="clientTypes" />
[AttributeUsage(AttributeTargets.Class)]
public sealed class ApiClientDelegatingHandleMetadataAttribute(
    int order,
    params Type[] clientTypes)
    : ApiClientHandlerMetadataAttributeBase(clientTypes)
{
    /// <summary>
    /// Порядок регистрации обработчика в конвейере.
    /// </summary>
    /// <value>
    /// Целочисленный приоритет: меньшие значения регистрируются раньше.
    /// </value>
    public int Order { get; } = order;
}
