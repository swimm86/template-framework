using Shared.Domain.Core.Exceptions.Models.Base;

namespace Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

internal sealed class TestAppException : AppException
{
    public TestAppException()
    {
    }

    public TestAppException(string message, IReadOnlyDictionary<string, object>? additionalData = null)
        : base(message, additionalData)
    {
    }

    public TestAppException(string message, Exception innerException, IReadOnlyDictionary<string, object>? additionalData = null)
        : base(message, innerException, additionalData)
    {
    }
}
