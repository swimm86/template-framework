// ----------------------------------------------------------------------------------------------
// <copyright file="ManualConfigurationAttribute.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.DependencyInjection.Attributes;

/// <summary>
/// Атрибут, наличие которого обозначает, что класс должен быть сконфигурирован вручную.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ManualConfigurationAttribute : Attribute
{
}
