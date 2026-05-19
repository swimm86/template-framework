using Shared.Application.Cqrs.Core.Abstractions.Queries.Requests;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public sealed class TestReadListQuery(TestPageableRequest request)
    : ReadListQuery<TestPageableRequest, TestListFilter, TestPageableResponse>(request);
