// ----------------------------------------------------------------------------------------------
// <copyright file="TestExceptionChainCommand.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dto.Responses;
using Shared.Application.Cqrs.Core.Abstractions.Commands;

namespace Template.Setter.Application.Abstractions.Features.Test.ExceptionChain;

public sealed record TestExceptionChainCommand
    : ICommand<Response>;
