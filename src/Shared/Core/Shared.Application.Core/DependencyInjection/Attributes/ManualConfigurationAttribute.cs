// ----------------------------------------------------------------------------------------------
// <copyright file="ManualConfigurationAttribute.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.DependencyInjection.Attributes;

/// <summary>
/// Указывает, что класс требует ручной конфигурации в DI-контейнере и должен быть исключён из автоматической регистрации.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ManualConfigurationAttribute
    : Attribute;
