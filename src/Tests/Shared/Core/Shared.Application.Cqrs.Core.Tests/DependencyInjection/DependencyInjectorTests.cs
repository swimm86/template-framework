using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Application.Core.Auth;
using Shared.Application.Cqrs.Core.Behaviours;
using Shared.Application.Cqrs.Core.DependencyInjection;
using Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Domain.Core.Mapping.Interfaces;
using Shared.Testing.Doubles.Mapping;
using Shared.Testing.Doubles.Repository;

namespace Shared.Application.Cqrs.Core.Tests.DependencyInjection;

/// <summary>
/// Тесты <see cref="DependencyInjector"/>.
/// Проверяют регистрацию MediatR-сервисов и pipeline behavior'ов.
/// </summary>
public sealed class DependencyInjectorTests
{
    #region Inject Tests

    /// <summary>
    /// <c>Inject</c> возвращает ту же коллекцию сервисов (fluent API).
    /// </summary>
    [Fact]
    public void Inject_ShouldReturnServiceCollection()
    {
        // Arrange
        var injector = new DependencyInjector(NullLoggerFactory.Instance);
        var services = new ServiceCollection();

        // Act
        var result = injector.Inject(services);

        // Assert
        result.Should().BeSameAs(services);
    }

    /// <summary>
    /// <c>Inject</c> регистрирует MediatR и позволяет отправить запрос
    /// через <see cref="IMediator"/> с разрешением всех зависимостей.
    /// </summary>
    [Fact]
    public async Task Inject_ShouldRegisterMediatrServices()
    {
        // Arrange
        var injector = new DependencyInjector(NullLoggerFactory.Instance);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IUnitOfWork>(_ => new FakeUnitOfWork());
        services.AddScoped<IMapper>(_ => new FakeMapper());
        services.AddScoped<IUserProvider>(_ => new FakeUserProvider());

        injector.Inject(services);

        // Act
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
        });

        provider.GetRequiredService<IMediator>().Should().NotBeNull();

        await using var scope = provider.CreateAsyncScope();
        var response = await scope.ServiceProvider
            .GetRequiredService<IMediator>()
            .Send(new TestRequest(), TestContext.Current.CancellationToken);

        // Assert
        response.Should().NotBeNull();
    }

    /// <summary>
    /// <c>Inject</c> регистрирует оба pipeline behavior'а
    /// (<see cref="LoggingPipelineBehaviour{TRequest,TResponse}"/> и
    /// <see cref="ValidationPipelineBehaviour{TRequest,TResponse}"/>) как scoped.
    /// </summary>
    [Fact]
    public void Inject_ShouldRegisterPipelineBehaviors()
    {
        // Arrange
        var injector = new DependencyInjector(NullLoggerFactory.Instance);
        var services = new ServiceCollection();

        // Act
        injector.Inject(services);

        // Assert
        var behaviorDescriptors = services
            .Where(s => s.ServiceType == typeof(IPipelineBehavior<,>))
            .ToList();

        behaviorDescriptors.Should().HaveCount(2);
        behaviorDescriptors.Should().AllSatisfy(d => d.Lifetime.Should().Be(ServiceLifetime.Scoped));
        behaviorDescriptors[0].ImplementationType.Should().Be(typeof(LoggingPipelineBehaviour<,>));
        behaviorDescriptors[1].ImplementationType.Should().Be(typeof(ValidationPipelineBehaviour<,>));
    }

    #endregion
}
