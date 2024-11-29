// ----------------------------------------------------------------------------------------------
// <copyright file="MigrationAssemblyAttribute.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Reflection;

namespace Shared.Infrastructure.Dal.EFCore.Attributes;

/// <summary>
/// Атрибут, который помечает <see cref="Assembly"/>> как миграционную.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public class MigrationAssemblyAttribute : Attribute;
