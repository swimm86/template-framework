// ----------------------------------------------------------------------------------------------
// <copyright file="GuardThrowingReadQueryHandler.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Shared.Application.Cqrs.Core.Abstractions.Queries.Handlers;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Domain.Core.Mapping.Interfaces;
using Shared.Testing.Entities;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public sealed class GuardThrowingReadQueryHandler(
    ILoggerFactory loggerFactory,
    IMapper mapper,
    IUnitOfWork unitOfWork)
    : ReadQueryHandler<TestReadByKeyQuery, TestEntity, TestEntity>(loggerFactory, mapper, unitOfWork)
{
    public Exception ExceptionToThrow { get; set; } = new InvalidOperationException("guard-failed");

    protected override Task GuardAsync(TestReadByKeyQuery request, CancellationToken cancellationToken)
    {
        throw ExceptionToThrow;
    }
}
