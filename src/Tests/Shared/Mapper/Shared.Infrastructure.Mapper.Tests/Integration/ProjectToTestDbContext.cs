// ----------------------------------------------------------------------------------------------
// <copyright file="ProjectToTestDbContext.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Shared.Domain.Core.Interfaces;

namespace Shared.Infrastructure.Mapper.Tests.Integration;

/// <summary>
/// Тестовый DbContext с <see cref="ProjectToTestEntity"/>, используемый в интеграционных тестах
/// <c>ProjectTo</c> для мапперов. Общий для всех реализаций (AutoMapper, Mapster и т.д.).
/// </summary>
public sealed class ProjectToTestDbContext(DbContextOptions<ProjectToTestDbContext> options)
    : DbContext(options)
{
    public DbSet<ProjectToTestEntity> Entities => Set<ProjectToTestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProjectToTestEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Name).HasMaxLength(256);
        });
    }
}

/// <summary>
/// Тестовая сущность для интеграционных тестов <c>ProjectTo</c>.
/// Реализует минимальный набор интерфейсов аудита из Domain.Core.
/// </summary>
public sealed class ProjectToTestEntity : IEntity<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid? CreatedByUserId { get; private set; }

    public string? CreatedByUserName { get; private set; }

    public DateTime DateCreated { get; private set; }

    public DateTime? DateUpdated { get; private set; }

    public DateTime? DateDeleted { get; private set; }

    public bool IsDeleted { get; private set; }

    public Guid? DeletedByUserId { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public string? UpdatedByUserName { get; private set; }

    public string[] RequiredToSaveNavigationPropertiesNames => [];

    public void SetDateCreated(DateTime dateCreated) => DateCreated = dateCreated;
    public void SetDateUpdated(DateTime? dateUpdated) => DateUpdated = dateUpdated;
    public void SetDateDeleted(DateTime? dateDeleted) => DateDeleted = dateDeleted;
    public void SetIsDeleted() => IsDeleted = true;
    public void SetCreatedByUserId(Guid? createdByUserId) => CreatedByUserId = createdByUserId;
    public void SetCreatedByUserName(string userName) => CreatedByUserName = userName;
    public void SetDeletedByUserId(Guid? deletedByUserId) => DeletedByUserId = deletedByUserId;
    public void SetUpdatedByUserId(Guid? updatedByUserId) => UpdatedByUserId = updatedByUserId;
    public void SetUpdatedByUserName(string userName) => UpdatedByUserName = userName;
}
