// ----------------------------------------------------------------------------------------------
// <copyright file="AppExceptionMapperBase.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Shared.Domain.Core.Exceptions.Models.Base;

namespace Shared.Presentation.Core.Exceptions.Mappers.Base;

/// <summary>
/// Базовый маппер для обработки исключений, унаследованных от <see cref="AppException"/>.
/// </summary>
/// <typeparam name="TException">Тип исключения, унаследованный от <see cref="AppException"/>.</typeparam>
public abstract class AppExceptionMapperBase<TException>(
    IConfiguration configuration)
    : ExceptionMapperBase<TException>(configuration)
    where TException : AppException
{
    /// <inheritdoc/>
    protected override IReadOnlyDictionary<string, object>? GetAdditionalData(
        TException exception)
    {
        return exception.AdditionalData;
    }
}
