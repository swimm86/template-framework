// ----------------------------------------------------------------------------------------------
// <copyright file="OneToOneConfiguration.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Domain.Entities;
using Shared.Infrastructure.Dal.EFCore.Configurations;

namespace Gpn.Template.Infrastructure.Dal.Configuration;

/// <summary>
/// Конфигурация сущности "OneToOne".
/// </summary>
public class OneToOneConfiguration : EntityConfigurationBase<OneToOne>;
