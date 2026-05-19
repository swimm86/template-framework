using Shared.Application.Core.Dto.Responses;
using Shared.Testing.Entities;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public sealed record TestPageableResponse : PageableResponse<ICollection<TestEntity>>;
