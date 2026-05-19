using Shared.Application.Core.Dto.Requests;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public sealed record TestListRequest : TestPageableRequest, IListRequest<TestListFilter>;
