using Shared.Application.Cqrs.Core.Abstractions.Commands.Requests;
using Shared.Application.Cqrs.Core.Abstractions.Commands.Responses;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public sealed record TestCloneResponse : CreateResponse<object>;

public sealed record TestCloneCommand(object Key, object Request) : CloneCommand<object, TestCloneResponse>(Key, Request);
