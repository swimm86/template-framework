// ----------------------------------------------------------------------------------------------
// <copyright file="ManualConfigurationAttribute.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Attributes;

/// <summary>
/// Атрибут, наличие которого обозначает, что класс должен быть сконфигурирован вручную.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ManualConfigurationAttribute : Attribute
{
}
