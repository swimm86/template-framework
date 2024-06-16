// ----------------------------------------------------------------------------------------------
// <copyright file="IDtoPostProcessor.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Cqrs.Core.Utils.PostProcessors;

/// <summary>
/// Интерфейс обработки ДТО перед возвращением из хендлера
/// </summary>
/// <typeparam name="TDto">ДТО</typeparam>
public interface IDtoPostProcessor<in TDto>
{
    /// <summary>
    /// Обработка множества.
    /// </summary>
    /// <param name="list">Список ДТО.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task HandleAsync(IEnumerable<TDto> list);
}
