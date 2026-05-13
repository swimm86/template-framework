// ----------------------------------------------------------------------------------------------
// <copyright file="ManualConfigurationAttribute.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.DependencyInjection.Attributes;

/// <summary>
/// Атрибут, наличие которого обозначает, что класс должен быть сконфигурирован вручную.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ManualConfigurationAttribute
    : Attribute;
