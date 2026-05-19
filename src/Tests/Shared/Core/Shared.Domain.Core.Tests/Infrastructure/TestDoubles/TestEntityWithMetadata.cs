using Shared.Domain.Core.Base;

namespace Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

public class TestEntityWithMetadata : EntityWithMetadata<TestEntityWithMetadata, Guid>
{
    public string Name { get; set; } = string.Empty;
}
