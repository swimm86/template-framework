using Shared.Domain.Core.Exceptions.Models.Base;

namespace Shared.Presentation.Core.Tests.Infrastructure.TestDoubles;

/// <summary>
/// Исключение для тестов <c>ExceptionMapperBase&lt;TException&gt;</c>.
/// </summary>
internal sealed class TestException : Exception
{
    /// <summary>
    /// Инициализация с сообщением.
    /// </summary>
    public TestException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Инициализация с сообщением и внутренним исключением.
    /// </summary>
    public TestException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

/// <summary>
/// Исключение-наследник <see cref="AppException"/> для тестов.
/// </summary>
internal sealed class TestAppException : AppException
{
    /// <summary>
    /// Инициализация с сообщением и дополнительными данными.
    /// </summary>
    public TestAppException(
        string message,
        IReadOnlyDictionary<string, object>? additionalData = null)
        : base(message, additionalData)
    {
    }
}
