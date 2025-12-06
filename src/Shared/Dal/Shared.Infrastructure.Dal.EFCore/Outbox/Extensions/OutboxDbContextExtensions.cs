// ----------------------------------------------------------------------------------------------
// <copyright file="OutboxDbContextExtensions.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Shared.Domain.Core.Entities;

namespace Shared.Infrastructure.Dal.EFCore.Outbox.Extensions;

/// <summary>
/// Расширения для DbContext для работы с Outbox.
/// </summary>
public static class OutboxDbContextExtensions
{
    /// <summary>
    /// Регистрирует сущность OutboxEvent в DbContext.
    /// </summary>
    /// <param name="modelBuilder">Model builder.</param>
    public static void ConfigureOutbox(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OutboxEventConfiguration());
    }

    /// <summary>
    /// Добавляет DbSet для OutboxEvent.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    /// <returns>DbSet для OutboxEvent.</returns>
    public static DbSet<OutboxEvent> OutboxEvents(this DbContext context)
    {
        return context.Set<OutboxEvent>();
    }
}

