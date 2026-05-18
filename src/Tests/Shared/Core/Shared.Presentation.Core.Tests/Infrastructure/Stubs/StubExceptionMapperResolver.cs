using Shared.Application.Core.Dto.Responses;
using Shared.Presentation.Core.Exceptions.Interfaces;

namespace Shared.Presentation.Core.Tests.Infrastructure.Stubs;

/// <summary>
/// Стаб <see cref="IExceptionMapperResolver"/> с подсчётом вызовов и сбором исключений.
/// </summary>
internal sealed class StubExceptionMapperResolver : IExceptionMapperResolver
{
    private readonly Func<Exception, ErrorResponse> _map;

    /// <summary>
    /// Инициализация стаба с произвольной логикой.
    /// </summary>
    public StubExceptionMapperResolver(Func<Exception, ErrorResponse> map) => _map = map;

    /// <summary>
    /// Инициализация стаба с фиксированным ответом.
    /// </summary>
    public StubExceptionMapperResolver(ErrorResponse response)
        : this(_ => response)
    {
    }

    /// <summary>
    /// Количество вызовов <see cref="Shared.Presentation.Core.Exceptions.Interfaces.IExceptionMapperResolver.Map"/>.
    /// </summary>
    public int CallCount { get; private set; }

    /// <summary>
    /// Список всех исключений, переданных в <see cref="Shared.Presentation.Core.Exceptions.Interfaces.IExceptionMapperResolver.Map"/>.
    /// </summary>
    public List<Exception> ReceivedExceptions { get; } = new();

    /// <inheritdoc />
    public ErrorResponse Map(Exception exception)
    {
        CallCount++;
        ReceivedExceptions.Add(exception);
        return _map(exception);
    }
}
