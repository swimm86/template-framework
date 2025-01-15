// ----------------------------------------------------------------------------------------------
// <copyright file="IWithOnSavingAction.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Interfaces;

/// <summary>
/// Инетерфейс для сущностей, которые выполняют действия перед сохранением изменений.
/// </summary>
public interface IWithOnSavingAction
{
    /// <summary>
    /// Признак того, что действия перед сохранением выполнены.
    /// </summary>
    bool IsOnSavingConfirmed { get; set; }

    /// <summary>
    /// Выполняет операции, которые необходимы при сохранением.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат асинхронной операции.</returns>
    public async Task OnSavingAsync(CancellationToken cancellationToken = default)
    {
        if (IsOnSavingConfirmed)
        {
            return;
        }

        await OnSavingProcessAsync(cancellationToken);
        IsOnSavingConfirmed = true;
    }

    /// <summary>
    /// Реализация операций, которые необходимо выполнить перед сохранением.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат асинхронной операции.</returns>
    protected Task OnSavingProcessAsync(CancellationToken cancellationToken);
}
