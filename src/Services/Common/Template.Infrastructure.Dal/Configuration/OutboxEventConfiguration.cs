// ----------------------------------------------------------------------------------------------
// <copyright file="OutboxEventConfiguration.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Domain.Core.Entities;
using Shared.Infrastructure.Dal.EFCore.Configurations;

namespace Template.Infrastructure.Dal.Configuration;

/// <summary>
/// Конфигурация для сущности OutboxEvent.
/// </summary>
public class OutboxEventConfiguration
    : EntityConfigurationBase<OutboxEvent>
{
    /// <inheritdoc />
    protected override void ConfigureProcess(EntityTypeBuilder<OutboxEvent> builder)
    {
        base.ConfigureProcess(builder);

        builder.ToTable(
            "outbox_events",
            t => t.HasComment("Таблица событий Outbox для надежной доставки сообщений."));

        builder
            .Property(x => x.EventType)
            .IsRequired()
            .HasMaxLength(255)
            .HasComment("Тип события для маршрутизации.");

        builder
            .Property(x => x.EventData)
            .IsRequired()
            .HasColumnType("text")
            .HasComment("JSON-данные события.");

        builder
            .Property(x => x.CorrelationId)
            .HasMaxLength(255)
            .HasComment("Идентификатор корреляции для связывания событий.");

        builder
            .Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasComment("Текущий статус события.");

        builder
            .Property(x => x.CreatedAt)
            .IsRequired()
            .HasComment("Время создания события.");

        builder
            .Property(x => x.ProcessedAt)
            .HasComment("Время успешной обработки события.");

        builder
            .Property(x => x.RetryCount)
            .IsRequired()
            .HasDefaultValue(0)
            .HasComment("Количество попыток обработки.");

        builder
            .Property(x => x.ErrorMessage)
            .HasMaxLength(2000)
            .HasComment("Сообщение об ошибке при неудачной обработке.");

        builder
            .Property(x => x.Url)
            .HasMaxLength(2000)
            .HasComment("URL для HTTP-запроса.");

        builder
            .Property(x => x.HttpMethod)
            .HasMaxLength(16)
            .HasComment("HTTP-метод (GET, POST, PUT, DELETE).");

        builder
            .Property(x => x.HeadersJson)
            .HasColumnType("text")
            .HasComment("JSON-сериализованные HTTP-заголовки.");

        builder
            .Property(x => x.ContentType)
            .HasMaxLength(255)
            .HasComment("Тип контента HTTP-запроса.");

        builder
            .Property(x => x.TimeoutSeconds)
            .IsRequired()
            .HasDefaultValue(100)
            .HasComment("Таймаут HTTP-запроса в секундах.");

        builder
            .Property(x => x.MaxRetryCount)
            .IsRequired()
            .HasDefaultValue(5)
            .HasComment("Максимальное количество попыток.");

        builder
            .Property(x => x.NextAttemptAt)
            .HasComment("Время следующей попытки обработки.");

        builder
            .Property(x => x.Priority)
            .IsRequired()
            .HasDefaultValue(0)
            .HasComment("Приоритет события (0 - наивысший).");

        builder
            .Property(x => x.IdempotencyKey)
            .HasMaxLength(255)
            .HasComment("Ключ идемпотентности для предотвращения дублирования.");

        builder
            .Property(x => x.TraceId)
            .HasMaxLength(255)
            .HasComment("Идентификатор трейса для распределенного трейсинга.");

        builder
            .Property(x => x.TenantId)
            .HasMaxLength(255)
            .HasComment("Идентификатор арендатора для мультитенантности.");

        builder
            .Property(x => x.LockId)
            .HasMaxLength(255)
            .HasComment("Идентификатор блокировки для конкурентной обработки.");

        builder
            .Property(x => x.LockExpiresAt)
            .HasComment("Время истечения блокировки.");

        // Индексы
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("ix_outbox_events_status");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("ix_outbox_events_created_at");

        builder.HasIndex(e => e.CorrelationId)
            .HasDatabaseName("ix_outbox_events_correlation_id");

        builder.HasIndex(e => e.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("ix_outbox_events_idempotency_key");

        builder.HasIndex(e => e.NextAttemptAt)
            .HasDatabaseName("ix_outbox_events_next_attempt_at");

        builder.HasIndex(e => e.Priority)
            .HasDatabaseName("ix_outbox_events_priority");

        builder.HasIndex(e => new { e.Status, e.NextAttemptAt })
            .HasDatabaseName("ix_outbox_events_status_next_attempt");
    }
}
