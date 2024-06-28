// ----------------------------------------------------------------------------------------------
// <copyright file="EnvPathAttribute.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.Configuration.Attributes;

/// <summary>
/// Атрибут для указания пути .env-файла с конфигурацией.
/// </summary>
/// <param name="path">Относительный путь .env-файла с конфигурацией.</param>
[AttributeUsage(AttributeTargets.Assembly)]
public class EnvPathAttribute(string path) : Attribute
{
    /// <summary>
    /// Относительный путь .env-файла с конфигурацией.
    /// </summary>
    public readonly string Path = path;
}
