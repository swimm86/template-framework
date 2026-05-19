using Shared.Domain.Core.Base;

namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public sealed class TestEntityWithoutDateCreated : BaseEntity<Guid>
{
    public string Name { get; set; } = string.Empty;
}
