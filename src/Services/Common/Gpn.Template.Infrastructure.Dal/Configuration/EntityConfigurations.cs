// ----------------------------------------------------------------------------------------------
// <copyright file="EntityConfigurations.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Domain.Entities;
using Shared.Infrastructure.Dal.EFCore.Configurations;

namespace Gpn.Template.Infrastructure.Dal.Configuration;

/// <summary>
/// Конфигурация сущности "Person".
/// </summary>
public class EntityConfiguration : EntityConfigurationBase<Person>
{
}
