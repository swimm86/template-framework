using Shared.Application.Cqrs.Core.Abstractions.Commands.Requests;
using Shared.Application.Cqrs.Core.Abstractions.Commands.Responses;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public sealed record TestUpdateResponse : UpdateResponse<object>;

public sealed record TestUpdateCommand(object Key, object Request) : UpdateCommand<object, TestUpdateResponse>(Key, Request);
