using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Core.Batch.Http.RetryPolicy.Models;
using Shared.Testing.DependencyInjection;

namespace Shared.Application.Core.Tests.DependencyInjection;

/// <summary>
/// Дымовые тесты тестовой инфраструктуры DI: сборка <see cref="ServiceProvider"/> без ошибок и замена singleton.
/// </summary>
/// <remarks>
/// Связано с <see cref="Shared.Testing.DependencyInjection.ServiceProviderBuilder"/> и регистрацией типов из приложения (например <see cref="RetryConfiguration"/>).
/// </remarks>
public sealed class ServiceProviderInfrastructureTests
{
    private interface IProbe
    {
        int Value { get; }
    }

    private sealed class ProbeA : IProbe
    {
        public int Value => 1;
    }

    private sealed class ProbeB : IProbe
    {
        public int Value => 2;
    }

    /// <summary>
    /// Ожидаемое значение зонда и признак замены зарегистрированной реализации singleton через <c>ReplaceSingleton</c>.
    /// </summary>
    public static TheoryData<int, bool> ProbeResolutionCases { get; } = new()
    {
        { 1, false },
        { 2, true },
    };

    /// <summary>
    /// Контейнер собирается без исключений; при <paramref name="replaceWithB"/> заменяется singleton и разрешается ожидаемая реализация <see cref="IProbe"/>.
    /// </summary>
    /// <param name="expectedValue">1 для <see cref="ProbeA"/>, 2 для <see cref="ProbeB"/>.</param>
    /// <param name="replaceWithB">Если <see langword="true"/> — после базовой регистрации singleton заменяется на <see cref="ProbeB"/>.</param>
    [Theory]
    [MemberData(nameof(ProbeResolutionCases))]
    public void Build_WithOptionalReplaceSingleton_ResolvesExpectedValue(int expectedValue, bool replaceWithB)
    {
        // Arrange
        using var provider = ServiceProviderBuilder.Build(services =>
        {
            services.AddSingleton<IProbe, ProbeA>();
            if (replaceWithB)
            {
                services.ReplaceSingleton<IProbe>(new ProbeB());
            }
        });

        // Act
        var probe = provider.GetRequiredService<IProbe>();

        // Assert
        probe.Value.Should().Be(expectedValue);
    }

    /// <summary>
    /// Регистрация <see cref="RetryConfiguration"/> как в приложении: значение корректно разрешается из контейнера.
    /// </summary>
    [Fact]
    public void Build_RegistersPageableRetryOptionsAsInAppComposition()
    {
        // Arrange
        using var provider = ServiceProviderBuilder.Build(services =>
        {
            services.AddSingleton(_ => new RetryConfiguration
            {
                Backoff = new BackoffConfiguration
                {
                    MaxAttempts = 3,
                    InitialDelay = TimeSpan.FromMilliseconds(1),
                },
            });
        });

        // Act
        var options = provider.GetRequiredService<RetryConfiguration>();

        // Assert
        options.Backoff.MaxAttempts.Should().Be(3);
    }
}
