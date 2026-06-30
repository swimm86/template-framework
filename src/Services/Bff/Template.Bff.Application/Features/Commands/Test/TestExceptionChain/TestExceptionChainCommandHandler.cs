// ----------------------------------------------------------------------------------------------
// <copyright file="TestExceptionChainCommandHandler.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dto.Responses;
using Shared.Application.Cqrs.Core.Abstractions.Commands.Handlers;
using Template.Bff.Application.Interfaces.HttpClients;
using Template.Setter.Application.Abstractions.Features.Test.ExceptionChain;

namespace Template.Bff.Application.Features.Commands.Test.TestExceptionChain;

/// <inheritdoc />
public class TestExceptionChainCommandHandler(
    ISetterClient setterClient)
    : ICommandHandler<TestExceptionChainCommand, Response>
{
/// <inheritdoc />
    public Task<Response> Handle(
        TestExceptionChainCommand command,
        CancellationToken cancellationToken)
    {
        return setterClient.TestExceptionChainAsync(cancellationToken);
    }
}
