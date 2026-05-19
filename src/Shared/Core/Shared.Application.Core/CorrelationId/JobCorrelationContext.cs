// ----------------------------------------------------------------------------------------------
// <copyright file="JobCorrelationContext.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.CorrelationId;

/// <summary>
/// Статический контекст для хранения идентификатора корреляции фоновых задач.
/// Используется для отслеживания выполнения фоновых задач в системе.
/// Идентификатор корреляции устанавливается при запуске задачи и доступен в течение всего времени её выполнения.
/// </summary>
public static class JobCorrelationContext
{
    private static readonly AsyncLocal<Guid?> CorrelationId = new();

    /// <summary>
    /// Возвращает идентификатор корреляции фоновой задачи.
    /// </summary>
    /// <returns>Идентификатор корреляции или <see langword="null"/>, если не был установлен.</returns>
    public static Guid? GetCorrelationId()
    {
        return CorrelationId.Value;
    }

    /// <summary>
    /// Устанавливает идентификатор корреляции фоновой задачи.
    /// Идентификатор можно установить только один раз — повторная установка игнорируется.
    /// </summary>
    /// <returns><c>true</c>, если идентификатор был успешно установлен; <c>false</c>, если он уже установлен.</returns>
    public static bool TrySetCorrelationId()
    {
        if (CorrelationId.Value.HasValue)
        {
            return false;
        }

        CorrelationId.Value = Guid.NewGuid();
        return true;
    }

    /// <summary>
    /// Очищает идентификатор корреляции фоновой задачи.
    /// </summary>
    public static void ClearCorrelationId()
    {
        CorrelationId.Value = null;
    }
}
