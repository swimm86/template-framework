// ----------------------------------------------------------------------------------------------
// <copyright file="PersonCreateCommandHandler.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Auth;
using Shared.Application.Cqrs.Core.Abstractions.Commands.Handlers;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Domain.Core.Mapping.Interfaces;
using Template.Application.Dto.Person;
using Template.Setter.Application.Abstractions.Features.Person.Create;
using Template.Setter.Application.Abstractions.Features.Person.Create.Request;
using Template.Setter.Application.Abstractions.Features.Person.Create.Response;

namespace Template.Setter.Application.Features.Person.Create;

/// <summary>
/// Обработчик команды создания сущности 'Персона'.
/// </summary>
/// <param name="loggerFactory">Фабрика логгеров.</param>
/// <param name="mapper">Преобразователь объектов.</param>
/// <param name="unitOfWork">Unit of Work для управления транзакциями.</param>
/// <param name="validators">Коллекция валидаторов сущности.</param>
/// <param name="userProvider">Провайдер информации о текущем пользователе.</param>
public class PersonCreateCommandHandler(
    ILoggerFactory loggerFactory,
    IMapper mapper,
    IUnitOfWork unitOfWork,
    IEnumerable<IValidator<Domain.Entities.Person>> validators,
    IUserProvider userProvider)
    : CreateCommandHandler<
        PersonCreateCommand,
        PersonCreateRequest,
        Domain.Entities.Person,
        PersonDto,
        PersonCreateResponse>(
        loggerFactory,
        mapper,
        unitOfWork,
        validators,
        userProvider);
