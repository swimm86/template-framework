using Shared.Application.Core.DependencyInjection.Attributes;
using Shared.Application.Core.Dto.Responses;
using Shared.Presentation.Core.Exceptions.Interfaces;

namespace Shared.Presentation.Core.Tests.Infrastructure.Stubs;

/// <summary>
/// Стаб <see cref="IExceptionMapper"/> с контролируемым поведением.
/// </summary>
[ManualConfiguration]
internal sealed class StubExceptionMapper : IExceptionMapper
{
    private readonly Func<Exception, ErrorResponse> _map;

    /// <summary>
    /// Инициализация стаба с фиксированным ответом.
    /// </summary>
    public StubExceptionMapper(Type handledType, ErrorResponse fixedResponse)
        : this(handledType, _ => fixedResponse)
    {
    }

    /// <summary>
    /// Инициализация стаба с произвольной логикой маппинга.
    /// </summary>
    public StubExceptionMapper(Type handledType, Func<Exception, ErrorResponse> map)
    {
        HandledType = handledType;
        _map = map;
    }

    /// <inheritdoc />
    public Type HandledType { get; }

    /// <inheritdoc />
    public ErrorResponse Map(Exception exception) => _map(exception);
}
