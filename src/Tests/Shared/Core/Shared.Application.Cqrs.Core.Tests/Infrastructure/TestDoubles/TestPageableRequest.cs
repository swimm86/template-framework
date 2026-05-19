using Shared.Application.Core.Dto.Requests;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public record TestPageableRequest : PageableRequest<TestListFilter>;
