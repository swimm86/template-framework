using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Shared.Application.Core.DependencyInjection.Attributes;
using Shared.Domain.Core.Exceptions.Models.Base;
using Shared.Presentation.Core.Exceptions.Mappers.Base;

namespace Shared.Presentation.Core.Tests.Infrastructure.TestDoubles;

/// <summary>
/// Тестовый маппер для проверки <see cref="Shared.Presentation.Core.Exceptions.Mappers.Base.ExceptionMapperBase{TException}"/>.
/// </summary>
[ManualConfiguration]
internal sealed class TestExceptionMapper : ExceptionMapperBase<TestException>
{
    /// <inheritdoc />
    public TestExceptionMapper(IConfiguration configuration)
        : base(configuration)
    {
    }

    /// <inheritdoc />
    protected override string Title => "Test";

    /// <inheritdoc />
    protected override int GetResponseStatusCode(TestException exception)
        => StatusCodes.Status418ImATeapot;
}

/// <summary>
/// Тестовый маппер с отключённым обогащением трассировки.
/// </summary>
[ManualConfiguration]
internal sealed class TestExceptionMapperWithoutTrace : ExceptionMapperBase<TestException>
{
    /// <inheritdoc />
    public TestExceptionMapperWithoutTrace(IConfiguration configuration)
        : base(configuration)
    {
    }

    /// <inheritdoc />
    protected override string Title => "Test no-trace";

    /// <inheritdoc />
    protected override bool ShouldEnrichWithTrace => false;

    /// <inheritdoc />
    protected override int GetResponseStatusCode(TestException exception)
        => StatusCodes.Status418ImATeapot;
}

/// <summary>
/// Тестовый маппер для проверки <see cref="Shared.Presentation.Core.Exceptions.Mappers.Base.AppExceptionMapperBase{TException}"/>.
/// </summary>
[ManualConfiguration]
internal sealed class TestAppExceptionMapper : AppExceptionMapperBase<TestAppException>
{
    /// <inheritdoc />
    public TestAppExceptionMapper(IConfiguration configuration)
        : base(configuration)
    {
    }

    /// <inheritdoc />
    protected override string Title => "Test app";

    /// <inheritdoc />
    protected override int GetResponseStatusCode(TestAppException exception)
        => StatusCodes.Status418ImATeapot;
}
