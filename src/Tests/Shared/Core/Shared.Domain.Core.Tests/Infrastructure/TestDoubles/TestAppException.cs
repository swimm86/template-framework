using Shared.Domain.Core.Exceptions.Models.Base;

namespace Shared.Domain.Core.Tests.Infrastructure.TestDoubles;

internal sealed class TestAppException(
    string message,
    Exception? innerException = null,
    IReadOnlyDictionary<string, object>? additionalData = null)
    : AppException(message, innerException, additionalData);
