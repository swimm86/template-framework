// ----------------------------------------------------------------------------------------------
// <copyright file="ExceptionMapperSettings.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Presentation.Core.Exceptions.Mappers.Base;

namespace Shared.Presentation.Core.Exceptions.Settings;

/// <summary>
/// Настройка для <see cref="ExceptionMapperBase{TException}"/>
/// </summary>
/// <param name="ShouldEnrichWithTrace">
/// Определяет, нужно ли добавлять stack trace и детали исключения в ответ.
/// По умолчанию <c>true</c>; переопределите в <c>false</c> для исключений,
/// данные которых не должны обогащаться (например, проксированные ошибки).
/// </param>
/// <param name="StackTraceDepth">
/// Количество строк стека вызовов для отладки. Значение по-умолчанию (10)
/// выбрано для баланса между полезностью и размером ответа.
/// </param>
/// <param name="MaxExceptionDepth">
/// Максимальная глубина вложенности InnerException для защиты от циклических ссылок.
/// Значение 5 выбрано на основе анализа реальных инцидентов в highload-системах:
/// - 95% исключений укладываются в 1-2 уровня
/// - 99% исключений укладываются в 3-4 уровня
/// - 5 уровней покрывает edge-cases с AggregateException + ProxiedException + вложенные AppException
/// Увеличение глубины свыше 5 не даёт диагностической ценности, но растёт риск переполнения стека
/// и размер ответа при циклических ссылках.
/// </param>
public record ExceptionMapperSettings(
    bool ShouldEnrichWithTrace = true,
    int StackTraceDepth = 10,
    int MaxExceptionDepth = 5);
