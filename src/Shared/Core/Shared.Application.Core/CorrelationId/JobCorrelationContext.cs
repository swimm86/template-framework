// ----------------------------------------------------------------------------------------------
// <copyright file="JobCorrelationContext.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Application.Core.CorrelationId;

/// <summary>
/// Статический контекст для хранения идентификатора корреляции фоновых задач.
/// Используется для отслеживания корреляции выполнения фоновых задач в системе.
/// Идентификатор корреляции устанавливается при запуске джобы и доступен в течение всего времени её выполнения.
/// </summary>
public static class JobCorrelationContext
{
    private static readonly AsyncLocal<Guid?> CorrelationId = new();

    /// <summary>
    /// Получает идентификатор корреляции джобы.
    /// </summary>
    /// <returns>Идентификатор корреляции джобы или null, если не был установлен.</returns>
    public static Guid? GetCorrelationId()
    {
        return CorrelationId.Value;
    }

    /// <summary>
    /// Устанавливает идентификатор корреляции джобы.
    /// Идентификатор можно установить только один раз - повторная установка игнорируется, если значение уже установлено.
    /// </summary>
    /// <returns><c>true</c>, если идентификатор был успешно установлен, <c>false</c>, если идентификатор уже был установлен ранее.</returns>
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
    /// Очищает идентификатор корреляции джобы.
    /// </summary>
    public static void ClearCorrelationId()
    {
        CorrelationId.Value = null;
    }
}
