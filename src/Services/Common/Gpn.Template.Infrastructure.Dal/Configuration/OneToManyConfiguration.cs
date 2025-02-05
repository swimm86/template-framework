// ----------------------------------------------------------------------------------------------
// <copyright file="OneToManyConfiguration.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Domain.Entities;
using Shared.Infrastructure.Dal.EFCore.Configurations;

namespace Gpn.Template.Infrastructure.Dal.Configuration;

/// <summary>
/// Конфигурация сущности "OneToMany".
/// </summary>
public class OneToManyConfiguration : EntityConfigurationBase<OneToMany>;
