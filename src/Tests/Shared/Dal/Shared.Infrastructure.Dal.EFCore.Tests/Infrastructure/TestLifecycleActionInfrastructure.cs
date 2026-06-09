// ----------------------------------------------------------------------------------------------
// <copyright file="TestLifecycleActionInfrastructure.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Shared.Application.Core.DependencyInjection.Attributes;
using Shared.Application.Core.LifecycleAction;
using Shared.Domain.Core.Enums;
using Shared.Domain.Core.Interfaces;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

/// <summary>
/// Ключи тестовых действий жизненного цикла.
/// </summary>
public static class TestActionKeys
{
    public const string BeforeSaveEvent = "before-save-event";
    public const string AfterSaveEvent = "after-save-event";
}

/// <summary>
/// Тестовая сущность, используемая в сценариях lifecycle actions.
/// </summary>
public sealed class TestLifecycleActionEntity
    : IEntity<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    object IEntity.Id => Id;
}

/// <summary>
/// Тестовый обработчик BeforeSave для <see cref="TestLifecycleActionEntity"/>.
/// </summary>
public sealed class TestBeforeSaveHandler
    : LifecycleActionHandlerBase<TestLifecycleActionEntity>
{
    /// <summary>
    /// Количество вызовов <c>ExecuteActionAsync</c>.
    /// </summary>
    public int ExecuteCallCount { get; private set; }

    public override LifecyclePhase Phase => LifecyclePhase.BeforeSave;

    public override string Key => TestActionKeys.BeforeSaveEvent;

    public override int Order => 0;

    protected override Task ExecuteActionAsync(
        IEnumerable<TestLifecycleActionEntity> entities,
        CancellationToken cancellationToken)
    {
        ExecuteCallCount++;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Тестовый обработчик AfterSave для <see cref="TestLifecycleActionEntity"/>.
/// </summary>
public sealed class TestAfterSaveHandler
    : LifecycleActionHandlerBase<TestLifecycleActionEntity>
{
    /// <summary>
    /// Количество вызовов <c>ExecuteActionAsync</c>.
    /// </summary>
    public int ExecuteCallCount { get; private set; }

    public override LifecyclePhase Phase => LifecyclePhase.AfterSave;

    public override string Key => TestActionKeys.AfterSaveEvent;

    public override int Order => 0;

    protected override Task ExecuteActionAsync(
        IEnumerable<TestLifecycleActionEntity> entities,
        CancellationToken cancellationToken)
    {
        ExecuteCallCount++;
        return Task.CompletedTask;
    }
}

/// <summary>
/// DbContext для тестов действий жизненного цикла.
/// Помечен <see cref="ManualConfigurationAttribute"/> — не регистрируется автоматически.
/// </summary>
[ManualConfiguration]
public sealed class TestLifecycleActionDbContext(
    DbContextOptions<TestLifecycleActionDbContext> options)
    : DbContextBase(options, new FakeHostEnvironment())
{
    public DbSet<TestLifecycleActionEntity> DomainEntities => Set<TestLifecycleActionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestLifecycleActionEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
        });
    }
}
