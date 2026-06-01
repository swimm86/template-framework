// ----------------------------------------------------------------------------------------------
// <copyright file="PersonCreateCommandHandler.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Cqrs.Core.Abstractions.Commands.Handlers;
using Template.Bff.Application.Interfaces.HttpClients;
using Template.Setter.Application.Abstractions.Features.Person.Create;
using Template.Setter.Application.Abstractions.Features.Person.Create.Response;

namespace Template.Bff.Application.Features.Commands.Person.Create;

/// <summary>
/// Обработчик <see cref="PersonCreateCommand"/>.
/// </summary>
/// <param name="setterClient">API-клиент для Setter-а.</param>
public class PersonCreateCommandHandler(
    ISetterClient setterClient)
    : ICommandHandler<PersonCreateCommand, PersonCreateResponse>
{
    /// <inheritdoc />
    public Task<PersonCreateResponse> Handle(
        PersonCreateCommand command,
        CancellationToken cancellationToken)
    {
        return setterClient.CreatePersonAsync(command.Request, cancellationToken);
    }
}
