using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Infrastructure.Dal.EFCore.Extensions;
using Shared.Infrastructure.Dal.EFCore.Interfaces;
using Shared.Infrastructure.Dal.EFCore.Repository;
using Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Extensions;

/// <summary>
/// Тесты extension-метода <c>AddDbContext</c>.
/// Проверяют регистрацию <see cref="IDbContextFactory{TContext}"/>,
/// <see cref="IRepository{TEntity}"/>, <see cref="IUnitOfWork"/>
/// и разрешение <see cref="DbContext"/> из DI-контейнера.
/// </summary>
public sealed class ServiceCollectionExtensionsAddDbContextTests
{
    #region AddDbContext Tests

    /// <summary>
    /// <c>AddDbContext</c> регистрирует фабрику контекста,
    /// сам контекст, репозиторий и unit of work.
    /// </summary>
    [Fact]
    public void AddDbContext_RegistersDbContextFactoryRepositoryAndUnitOfWork()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment, FakeHostEnvironment>();
        services.AddSingleton<IDbContextOptionsBuilderInitializer, InMemoryDbContextOptionsBuilderInitializer>();

        // Act
        services.AddDbContext<InjectorTestDbSettings, InjectorTestDbContext>(
            typeof(InjectorTestDbContext).Assembly.FullName!);

        // Assert
        services.Should().Contain(d => d.ServiceType == typeof(IDbContextFactory<InjectorTestDbContext>));
        services.Should().Contain(d => d.ServiceType == typeof(InjectorTestDbContext));
        services.Should().Contain(d =>
            d.ServiceType == typeof(IRepository<>) &&
            d.ImplementationType == typeof(EfRepository<>));
        services.Should().Contain(d =>
            d.ServiceType == typeof(IUnitOfWork) &&
            d.ImplementationType == typeof(EfUnitOfWork<InjectorTestDbContext>));
    }

    /// <summary>
    /// После регистрации через <c>AddDbContext</c>
    /// <see cref="InjectorTestDbContext"/> успешно разрешается из DI
    /// с InMemory-провайдером.
    /// </summary>
    [Fact]
    public async Task AddDbContext_ServiceProvider_ResolvesDbContext()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment, FakeHostEnvironment>();
        services.AddSingleton<IDbContextOptionsBuilderInitializer, InMemoryDbContextOptionsBuilderInitializer>();
        services.AddDbContext<InjectorTestDbSettings, InjectorTestDbContext>(
            typeof(InjectorTestDbContext).Assembly.FullName!);

        // Act
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<InjectorTestDbContext>();

        // Assert
        context.Should().NotBeNull();
        context.Database.ProviderName.Should().Contain("InMemory");
    }

    #endregion
}
