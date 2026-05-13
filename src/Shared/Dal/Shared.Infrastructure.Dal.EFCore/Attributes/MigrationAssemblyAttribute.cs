// ----------------------------------------------------------------------------------------------
// <copyright file="MigrationAssemblyAttribute.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Reflection;

namespace Shared.Infrastructure.Dal.EFCore.Attributes;

/// <summary>
/// Атрибут, который помечает <see cref="Assembly"/>> как миграционную.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public class MigrationAssemblyAttribute : Attribute;
