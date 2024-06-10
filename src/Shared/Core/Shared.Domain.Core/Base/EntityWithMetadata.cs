// ----------------------------------------------------------------------------------------------
// <copyright file="EntityWithMetadata.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Base;

/// <summary>
/// Абстрактный базовый класс сущности с метаданными.
/// </summary>
/// <typeparam name="TKey">Тип ключа сущности.</typeparam>
public abstract class EntityWithMetadata<TKey>
    : BaseEntity<TKey>, IEntityWithMetadata
{
    /// <inheritdoc/>
    public Guid? CreatedByUserId { get; protected set; }

    /// <inheritdoc/>
    public DateTime DateCreated { get; protected set; }

    /// <inheritdoc/>
    public Guid? UpdatedByUserId { get; protected set; }

    /// <inheritdoc/>
    public DateTime? DateUpdated { get; protected set; }

    /// <inheritdoc/>
    public Guid? DeletedByUserId { get; protected set; }

    /// <inheritdoc/>
    public DateTime? DateDeleted { get; protected set; }

    /// <inheritdoc/>
    public bool IsDeleted { get; protected set; }

    /// <inheritdoc/>
    public virtual void SetCreatedByUserId(Guid? createdByUserId) => CreatedByUserId = createdByUserId;

    /// <inheritdoc/>
    public virtual void SetDateCreated(DateTime dateCreated) => DateCreated = dateCreated;

    /// <inheritdoc/>
    public virtual void OnCreate(Guid? userId)
    {
        SetCreatedByUserId(userId);
        SetDateCreated(DateTimeOffset.UtcNow.DateTime);
    }

    /// <inheritdoc/>
    public virtual void SetUpdatedByUserId(Guid? updatedByUserId) => UpdatedByUserId = updatedByUserId;

    /// <inheritdoc/>
    public virtual void SetDateUpdated(DateTime? dateUpdated) => DateUpdated = dateUpdated;

    /// <inheritdoc/>
    public virtual void OnUpdate(Guid? userId)
    {
        SetUpdatedByUserId(userId);
        SetDateUpdated(DateTimeOffset.UtcNow.DateTime);
    }

    /// <inheritdoc/>
    public virtual void SetDeletedByUserId(Guid? deletedByUserId) => DeletedByUserId = deletedByUserId;

    /// <inheritdoc/>
    public virtual void SetDateDeleted(DateTime? dateDeleted) => DateDeleted = dateDeleted;

    /// <inheritdoc/>
    public virtual void SetIsDeleted() => IsDeleted = true;

    /// <inheritdoc/>
    public virtual void OnDelete(Guid? userId)
    {
        SetDeletedByUserId(userId);
        SetDateDeleted(DateTimeOffset.UtcNow.DateTime);
    }
}
