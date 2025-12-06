// ----------------------------------------------------------------------------------------------
// <copyright file="OutboxEventConfiguration.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Domain.Core.Entities;
using Shared.Domain.Core.Enums;
using Shared.Infrastructure.Dal.EFCore.Configurations;

namespace Shared.Infrastructure.Dal.EFCore.Outbox;

/// <summary>
/// Конфигурация Entity Framework для сущности <see cref="OutboxEvent"/>.
/// </summary>
public class OutboxEventConfiguration : EntityConfigurationBase<OutboxEvent, Guid>
{
    /// <inheritdoc/>
    public override void Configure(EntityTypeBuilder<OutboxEvent> builder)
    {
        base.Configure(builder);

        builder.ToTable("outbox_events");

        // Primary Key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();

        // EventType
        builder.Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(255);

        // EventData - храним как text без ограничения длины
        builder.Property(e => e.EventData)
            .IsRequired()
            .HasColumnType("text");

        // CorrelationId
        builder.Property(e => e.CorrelationId)
            .HasMaxLength(255);

        // Status
        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<int>();

        // CreatedAt
        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // ProcessedAt
        builder.Property(e => e.ProcessedAt);

        // RetryCount
        builder.Property(e => e.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        // ErrorMessage
        builder.Property(e => e.ErrorMessage)
            .HasColumnType("text");

        // Url
        builder.Property(e => e.Url)
            .HasMaxLength(2048);

        // HttpMethod
        builder.Property(e => e.HttpMethod)
            .HasMaxLength(10);

        // HeadersJson
        builder.Property(e => e.HeadersJson)
            .HasColumnType("text");

        // ContentType
        builder.Property(e => e.ContentType)
            .HasMaxLength(255);

        // TimeoutSeconds
        builder.Property(e => e.TimeoutSeconds)
            .IsRequired()
            .HasDefaultValue(100);

        // MaxRetryCount
        builder.Property(e => e.MaxRetryCount)
            .IsRequired()
            .HasDefaultValue(5);

        // NextAttemptAt
        builder.Property(e => e.NextAttemptAt);

        // Priority
        builder.Property(e => e.Priority)
            .IsRequired()
            .HasDefaultValue(0);

        // IdempotencyKey
        builder.Property(e => e.IdempotencyKey)
            .HasMaxLength(255);

        // TraceId
        builder.Property(e => e.TraceId)
            .HasMaxLength(255);

        // TenantId
        builder.Property(e => e.TenantId)
            .HasMaxLength(255);

        // LockId
        builder.Property(e => e.LockId)
            .HasMaxLength(255);

        // LockExpiresAt
        builder.Property(e => e.LockExpiresAt);

        // Indexes для оптимизации запросов
        
        // Основной индекс для поиска событий на обработку
        builder.HasIndex(e => new { e.Status, e.NextAttemptAt })
            .HasDatabaseName("ix_outbox_events_status_nextattempt");

        // Индекс для очистки обработанных событий
        builder.HasIndex(e => new { e.Status, e.ProcessedAt })
            .HasDatabaseName("ix_outbox_events_status_processed");

        // Индекс для корреляции
        builder.HasIndex(e => e.CorrelationId)
            .HasDatabaseName("ix_outbox_events_correlation");

        // Уникальный индекс для IdempotencyKey (если задан)
        builder.HasIndex(e => e.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("uix_outbox_events_idempotency")
            .HasFilter("idempotency_key IS NOT NULL");

        // Индекс для приоритета и времени создания
        builder.HasIndex(e => new { e.Priority, e.CreatedAt })
            .HasDatabaseName("ix_outbox_events_priority_created");

        // Индекс для блокировок
        builder.HasIndex(e => e.LockExpiresAt)
            .HasDatabaseName("ix_outbox_events_lock_expires")
            .HasFilter("lock_expires_at IS NOT NULL");
    }
}

