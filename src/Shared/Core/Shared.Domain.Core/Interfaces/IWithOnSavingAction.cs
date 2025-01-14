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
    /// Действие перед сохранением.
    /// </summary>
    /// <returns>Результат асинхронной операции.</returns>
    Task OnSavingAsync();
}
