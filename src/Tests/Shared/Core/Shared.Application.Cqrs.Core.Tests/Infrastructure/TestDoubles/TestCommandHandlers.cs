using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Auth;
using Shared.Application.Cqrs.Core.Abstractions.Commands.Handlers;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Domain.Core.Mapping.Interfaces;
using Shared.Testing.Entities;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public sealed class TestCreateCommandHandler(
    ILoggerFactory loggerFactory,
    IMapper mapper,
    IUnitOfWork unitOfWork,
    IEnumerable<IValidator<TestEntity>> validators,
    IUserProvider userProvider)
    : CreateCommandHandler<TestCreateCommand, object, TestEntity, object, TestCreateResponse>(
        loggerFactory, mapper, unitOfWork, validators, userProvider);

public sealed class TestUpdateCommandHandler(
    ILoggerFactory loggerFactory,
    IMapper mapper,
    IUnitOfWork unitOfWork,
    IEnumerable<IValidator<TestEntity>> validators,
    IUserProvider userProvider)
    : UpdateCommandHandler<TestUpdateCommand, object, TestEntity, object, TestUpdateResponse>(
        loggerFactory, mapper, unitOfWork, validators, userProvider)
{
    public new QueryOptions<TestEntity> ConstructOptions(TestUpdateCommand request)
        => base.ConstructOptions(request);
}

public sealed class TestDeleteCommandHandler(
    IUnitOfWork unitOfWork,
    ILoggerFactory loggerFactory,
    IUserProvider userProvider)
    : DeleteCommandHandler<TestDeleteCommand, TestEntity>(unitOfWork, loggerFactory, userProvider);

public sealed class TestCloneCommandHandler(
    IMapper mapper,
    IUnitOfWork unitOfWork,
    ILoggerFactory loggerFactory,
    IEnumerable<IValidator<TestEntity>> validators,
    IUserProvider userProvider)
    : CloneCommandHandler<TestCloneCommand, object, TestEntity, object, TestCloneResponse>(
        mapper, unitOfWork, loggerFactory, validators, userProvider);
