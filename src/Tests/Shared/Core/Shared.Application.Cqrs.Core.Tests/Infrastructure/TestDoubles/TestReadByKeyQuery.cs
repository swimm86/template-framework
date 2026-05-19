using Shared.Application.Cqrs.Core.Abstractions.Queries.Requests;
using Shared.Testing.Entities;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public sealed class TestReadByKeyQuery(object key) : ReadByKeyQuery<TestEntity>(key);
