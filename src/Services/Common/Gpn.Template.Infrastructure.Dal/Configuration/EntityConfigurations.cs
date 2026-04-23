// ----------------------------------------------------------------------------------------------
// <copyright file="EntityConfigurations.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Domain.Entities;
using Shared.Infrastructure.Dal.EFCore.Configurations;

namespace Gpn.Template.Infrastructure.Dal.Configuration;

/// <summary>
/// Конфигурация сущности "Person".
/// </summary>
public class EntityConfiguration : EntityConfigurationBase<Person>;
