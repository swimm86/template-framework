// ----------------------------------------------------------------------------------------------
// <copyright file="ApiClientRegistrationAttribute.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.ApiClient.Attributes;

/// <summary>
/// Задает явную конфигурацию для автоматической DI-регистрации API-клиента.
/// </summary>
/// <remarks>
/// Атрибут используется методом <c>AddHttpClients</c> для связывания:
/// <list type="bullet">
/// <item><description>типа настроек клиента (<see cref="SettingsType"/>);</description></item>
/// <item><description>типа интерфейса, под которым клиент регистрируется в DI (<see cref="ApiClientInterfaceType"/>).</description></item>
/// </list>
/// Применяется только к конкретным наследникам <c><see cref="ApiClient"/></c>.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ApiClientRegistrationAttribute(
    Type settingsType,
    Type interfaceType)
    : Attribute
{
    /// <summary>
    /// Тип настроек, к которому привязан API-клиент.
    /// </summary>
    public Type SettingsType { get; } = settingsType;

    /// <summary>
    /// Тип интерфейса, под которым API-клиент будет зарегистрирован в DI.
    /// </summary>
    public Type ApiClientInterfaceType { get; } = interfaceType;
}
