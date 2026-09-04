// ----------------------------------------------------------------------------------------------
// <copyright file="TestDeleteCommand.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Cqrs.Core.Abstractions.Commands.Requests;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public sealed record TestDeleteCommand(object Key) : DeleteCommand(Key);
