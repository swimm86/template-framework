using Shared.Application.Cqrs.Core.Behaviours;
using Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;
using Shared.Testing.Doubles.Logging;

namespace Shared.Application.Cqrs.Core.Tests.Behaviours;

public sealed class LoggingPipelineBehaviourTests
{
    private static LoggingPipelineBehaviour<TestRequest, TestResponse> CreateSut(
        FakeLogger<LoggingPipelineBehaviour<TestRequest, TestResponse>>? logger = null)
    {
        return new LoggingPipelineBehaviour<TestRequest, TestResponse>(
            logger ?? new FakeLogger<LoggingPipelineBehaviour<TestRequest, TestResponse>>(new FakeLogger()));
    }

    [Fact]
    public async Task Handle_CallsNextDelegate()
    {
        var sut = CreateSut();
        var request = new TestRequest();
        var response = new TestResponse();
        var called = false;

        Task<TestResponse> Next()
        {
            called = true;
            return Task.FromResult(response);
        }

        var result = await sut.Handle(request, Next, CancellationToken.None);

        called.Should().BeTrue();
        result.Should().Be(response);
    }

    [Fact]
    public async Task Handle_ReturnsResponseFromNext()
    {
        var sut = CreateSut();
        var request = new TestRequest();
        var expected = new TestResponse();

        Task<TestResponse> Next() => Task.FromResult(expected);

        var result = await sut.Handle(request, Next, CancellationToken.None);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task Handle_NextDelegateIsInvokedOnce()
    {
        var sut = CreateSut();
        var request = new TestRequest();
        var response = new TestResponse();
        var counter = 0;

        Task<TestResponse> Next()
        {
            counter++;
            return Task.FromResult(response);
        }

        await sut.Handle(request, Next, CancellationToken.None);

        counter.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToNext()
    {
        var sut = CreateSut();
        var request = new TestRequest();
        var response = new TestResponse();

        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        cts.Cancel();

        Task<TestResponse> Next() => Task.FromResult(response);

        var action = () => sut.Handle(request, Next, token);

        await action.Should().ThrowAsync<OperationCanceledException>();
    }
}
