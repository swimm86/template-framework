// ----------------------------------------------------------------------------------------------
// <copyright file="ApiClientHandlerMetadataAttribute.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.ApiClient.Handlers.Attributes;

/// <summary>
/// Задает метаданные обработчика исходящих HTTP-запросов для пайплайна API-клиента.
/// </summary>
/// <param name="order">
/// Порядок регистрации обработчика в конвейере.
/// Меньшее значение означает более раннее выполнение.
/// </param>
/// <param name="clientTypes">
/// Ограничение по типам API-клиентов.
/// Если массив пуст, обработчик применяется ко всем API-клиентам.
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
[AttributeUsage(AttributeTargets.Class)]
public sealed class ApiClientHandlerMetadataAttribute(
    int order,
    params Type[] clientTypes)
    : Attribute
{
    /// <summary>
    /// Порядок регистрации обработчика в конвейере.
    /// </summary>
    /// <value>
    /// Целочисленный приоритет: меньшие значения регистрируются раньше.
    /// </value>
    public int Order { get; } = order;

    /// <summary>
    /// Типы API-клиентов, к которым применяется обработчик.
    /// </summary>
    /// <value>
    /// Набор типов API-клиентов; пустой набор означает отсутствие ограничений.
    /// </value>
    public IReadOnlyCollection<Type> ClientTypes { get; } = clientTypes;

    /// <summary>
    /// Определяет, применяется ли обработчик к указанному типу API-клиента.
    /// </summary>
    /// <param name="apiClientType">Тип API-клиента для проверки применимости.</param>
    /// <returns>
    /// <see langword="true"/>, если <see cref="ClientTypes"/> пуст
    /// или содержит базовый/совместимый тип для <paramref name="apiClientType"/>; иначе <see langword="false"/>.
    /// </returns>
    public bool AppliesTo(Type apiClientType)
    {
        return ClientTypes.Count == 0 || ClientTypes.Any(type => type.IsAssignableFrom(apiClientType));
    }
}
