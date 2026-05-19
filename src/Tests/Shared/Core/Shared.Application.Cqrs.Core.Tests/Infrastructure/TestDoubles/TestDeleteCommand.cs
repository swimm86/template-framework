using Shared.Application.Cqrs.Core.Abstractions.Commands.Requests;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public sealed record TestDeleteCommand(object Key) : DeleteCommand(Key);
