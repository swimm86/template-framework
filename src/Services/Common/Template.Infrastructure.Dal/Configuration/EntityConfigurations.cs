// ----------------------------------------------------------------------------------------------
// <copyright file="EntityConfigurations.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Infrastructure.Dal.EFCore.Configurations;
using Template.Domain.Entities;

namespace Template.Infrastructure.Dal.Configuration;

/// <summary>
/// Конфигурация сущности "Person".
/// </summary>
public class EntityConfiguration : EntityConfigurationBase<Person>;
