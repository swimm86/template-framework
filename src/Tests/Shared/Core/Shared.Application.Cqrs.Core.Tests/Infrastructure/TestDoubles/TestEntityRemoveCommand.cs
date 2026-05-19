using Shared.Application.Cqrs.Core.Features.Entity.Remove;
using Shared.Application.Cqrs.Core.Features.Entity.Remove.Request;
using Shared.Testing.Entities;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public sealed record TestEntityRemoveCommand(EntityRemoveRequest Request) : EntityRemoveCommand<TestEntity>(Request);
