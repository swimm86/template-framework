using Shared.Application.Cqrs.Core.Abstractions.Commands.Requests;
using Shared.Application.Cqrs.Core.Abstractions.Commands.Responses;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public sealed record TestCreateResponse : CreateResponse<object>;

public sealed record TestCreateCommand(object Request) : CreateCommand<object, TestCreateResponse>(Request);
