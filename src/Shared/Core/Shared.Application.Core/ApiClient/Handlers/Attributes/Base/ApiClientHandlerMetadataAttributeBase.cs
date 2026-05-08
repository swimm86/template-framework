// ----------------------------------------------------------------------------------------------
// <copyright file="ApiClientHandlerMetadataAttributeBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.ApiClient.Handlers.Attributes.Base;

/// <summary>
/// Базовый абстрактный класс для аттрибутов, которые задают метаданные обработчиков исходящих HTTP-запросов для пайплайна API-клиента.
/// </summary>
/// <param name="clientTypes">
/// Ограничение по типам API-клиентов.
/// Если массив пуст, обработчик применяется ко всем API-клиентам.
/// </param>
[AttributeUsage(AttributeTargets.Class)]
public abstract class ApiClientHandlerMetadataAttributeBase(
    params Type[] clientTypes)
    : Attribute
{
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