// ----------------------------------------------------------------------------------------------
// <copyright file="TestExceptionChainCommandHandler.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dto.Responses;
using Shared.Application.Cqrs.Core.Abstractions.Commands.Handlers;
using Template.Setter.Application.Abstractions.Features.Test.ExceptionChain;
using Template.Setter.Application.Interfaces.HttpClients;

namespace Template.Setter.Application.Features.Test.ExceptionChain;

/// <inheritdoc />
public class TestExceptionChainCommandHandler(
    IGetterClient getterClient)
    : ICommandHandler<TestExceptionChainCommand, Response>
{
    /// <inheritdoc />
    public Task<Response> Handle(
        TestExceptionChainCommand command,
        CancellationToken cancellationToken)
    {
        return getterClient.TestExceptionChainAsync(cancellationToken);
    }
}
