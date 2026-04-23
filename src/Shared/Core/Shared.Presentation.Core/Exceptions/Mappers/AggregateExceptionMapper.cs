// ----------------------------------------------------------------------------------------------
// <copyright file="AggregateExceptionMapper.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Presentation.Core.Exceptions.Interfaces;
using Shared.Presentation.Core.Exceptions.Mappers.Base;

namespace Shared.Presentation.Core.Exceptions.Mappers;

/// <summary>
/// Маппер агрегированных исключений <see cref="AggregateException"/> в набор <see cref="ProblemDetails"/>.
/// </summary>
/// <remarks>
/// <para>
/// Делегирует маппинг каждого внутреннего исключения соответствующему <see cref="IExceptionMapper"/>
/// через <see cref="IExceptionMapperResolver"/>. Использует <see cref="AggregateException.Flatten"/>
/// для раскрытия вложенных <see cref="AggregateException"/>, предотвращая рекурсию.
/// </para>
/// <para>
/// Результирующий <see cref="Application.Core.Dto.Responses.ErrorResponse.StatusCode"/> всегда 500,
/// поскольку агрегированная ошибка не может быть представлена одним HTTP-статусом.
/// Каждый <see cref="ProblemDetails"/> содержит собственный статус внутреннего исключения.
/// </para>
/// <para>
/// Для избежания circular dependency (Dispatcher → Mappers → AggregateMapper → Dispatcher)
/// используется <see cref="IServiceProvider"/> для lazy-резолва диспетчера при первом вызове.
/// </para>
/// </remarks>
/// <param name="configuration">Конфигурация приложения.</param>
/// <param name="serviceProvider">Провайдер сервисов для резолва диспетчера.</param>
public sealed class AggregateExceptionMapper(
    IConfiguration configuration,
    IServiceProvider serviceProvider)
    : ExceptionMapperBase<AggregateException>(configuration)
{
    private IExceptionMapperResolver? _resolver;

    /// <inheritdoc/>
    protected override string Title => "Ошибка сервера";

    /// <inheritdoc/>
    protected override int GetResponseStatusCode(AggregateException exception)
        => StatusCodes.Status500InternalServerError;

    /// <inheritdoc/>
    protected override IReadOnlyCollection<ProblemDetails> GetProblemDetails(
        AggregateException exception)
    {
        var innerExceptions = exception.Flatten().InnerExceptions;
        if (innerExceptions.Count == 0)
        {
            return base.GetProblemDetails(exception);
        }

        _resolver ??= serviceProvider.GetRequiredService<IExceptionMapperResolver>();

        return innerExceptions
            .SelectMany(inner => _resolver.Map(inner).Errors)
            .ToArray();
    }
}
