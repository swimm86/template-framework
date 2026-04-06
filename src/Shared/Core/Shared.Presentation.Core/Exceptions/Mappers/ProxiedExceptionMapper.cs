// ----------------------------------------------------------------------------------------------
// <copyright file="ProxiedExceptionMapper.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Shared.Application.Core.Exceptions.Models;
using Shared.Presentation.Core.Exceptions.Mappers.Base;

namespace Shared.Presentation.Core.Exceptions.Mappers;

/// <summary>
/// Маппер проксированных исключений <see cref="ProxiedException"/> в <see cref="ProblemDetails"/>.
/// </summary>
/// <remarks>
/// <para>
/// Наследует <see cref="AppExceptionMapperBase{TException}"/>, что обеспечивает передачу
/// <see cref="Shared.Domain.Core.Exceptions.Models.Base.AppException.AdditionalData"/>
/// через <see cref="ExceptionMapperBase{TException}.GetAdditionalData"/>.
/// </para>
/// <para>
/// Подавляет обогащение stack trace (<see cref="ShouldEnrichWithTrace"/> = <c>false</c>),
/// поскольку проксированные ошибки содержат данные от upstream-сервиса, а не локальный stack trace.
/// </para>
/// </remarks>
public sealed class ProxiedExceptionMapper(
    IConfiguration configuration)
    : AppExceptionMapperBase<ProxiedException>(configuration)
{
    /// <inheritdoc/>
    protected override string Title => string.Empty;

    /// <inheritdoc/>
    protected override bool ShouldEnrichWithTrace => false;

    /// <inheritdoc/>
    protected override int GetResponseStatusCode(
        ProxiedException exception) => exception.StatusCode;

    /// <inheritdoc/>
    protected override IReadOnlyCollection<ProblemDetails> GetProblemDetails(
        ProxiedException exception)
    {
        var source = exception.ProblemDetails;
        var result = new ProblemDetails
        {
            Status = GetResponseStatusCode(exception),
            Title = source.Title,
            Detail = source.Detail,
            Instance = source.Instance,
            Type = source.Type,
            Extensions = source.Extensions,
        };

        return [result];
    }
}
