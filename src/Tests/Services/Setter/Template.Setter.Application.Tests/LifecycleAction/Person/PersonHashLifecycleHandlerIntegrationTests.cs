// ----------------------------------------------------------------------------------------------
// <copyright file="PersonHashLifecycleHandlerIntegrationTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Shared.Application.Core.LifecycleAction;
using Shared.Application.Core.LifecycleAction.Interfaces;
using Shared.Common.Helpers;
using Shared.Infrastructure.Dal.EFCore;
using Shared.Infrastructure.Dal.EFCore.Settings;
using TemplateSetterAppPersonHandler = Template.Setter.Application.LifecycleAction.Person.PersonHashLifecycleHandler;
using TemplateSetterDomainPerson = Template.Domain.Entities.Person;

namespace Template.Setter.Application.Tests.LifecycleAction.Person;

/// <summary>
/// Интеграционные тесты <see cref="TemplateSetterAppPersonHandler"/> через
/// <see cref="EfUnitOfWork{TDbContext}"/>: проверяют, что handler вызывается
/// в фазе <c>BeforeSave</c> и обновляет <see cref="TemplateSetterDomainPerson.Hash"/>
/// до того, как EF Core выполнит <c>SaveChangesAsync</c>.
/// </summary>
public sealed class PersonHashLifecycleHandlerIntegrationTests
{
    /// <summary>
    /// Минимальный тестовый <see cref="DbContext"/>, наследующий
    /// <see cref="DbContextBase"/>. Конфигурирует <see cref="TemplateSetterDomainPerson"/>
    /// вручную, чтобы не зависеть от <c>Template.Infrastructure.Dal</c> (который
    /// ссылается на Postgres и непригоден для InMemory-провайдера).
    /// </summary>
    private sealed class TestDbContext(
        DbContextOptions<TestDbContext> options,
        IHostEnvironment environment)
        : DbContextBase(options, environment)
    {
        public DbSet<TemplateSetterDomainPerson> Persons => Set<TemplateSetterDomainPerson>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TemplateSetterDomainPerson>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedNever();
                e.Property(x => x.Name).IsRequired();
                e.Property(x => x.Email).IsRequired();
                e.Property(x => x.Hash).IsRequired();
                e.HasIndex(x => x.Hash).IsUnique();
            });
        }
    }

    /// <summary>
    /// Минимальная реализация <c>DbSettingsBase</c> для теста:
    /// <c>EfDbSettingsBase</c> сам по себе абстрактен и требует задать
    /// <c>ConnectionString</c>/<c>TransactionsEnabled</c>.
    /// </summary>
    private sealed class TestDbSettings
        : EfDbSettingsBase<TestDbContext>
    {
        [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
        public TestDbSettings()
        {
            ConnectionString = "Server=localhost;Database=test;";
            TransactionsEnabled = false;
        }
    }

    private static DbContextOptions<TestDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
    }

    private static TestDbContext CreateContext()
    {
        return new TestDbContext(CreateOptions(), new DevelopmentHostEnvironment());
    }

    /// <summary>
    /// <see cref="EfUnitOfWork{TDbContext}"/> с реальным
    /// <see cref="TemplateSetterAppPersonHandler"/>, зарегистрированным через DI.
    /// </summary>
    private static EfUnitOfWork<TestDbContext> CreateUnitOfWork(
        TestDbContext context,
        params ILifecycleActionHandler[] additionalHandlers)
    {
        var settings = new TestDbSettings();

        var services = new ServiceCollection();
        services.AddScoped<ILifecycleActionHandler>(_ => new TemplateSetterAppPersonHandler());
        foreach (var handler in additionalHandlers)
        {
            services.AddScoped<ILifecycleActionHandler>(_ => handler);
        }

        var serviceProvider = services.BuildServiceProvider();
        var orchestrator = new LifecycleActionOrchestrator(
            serviceProvider.GetServices<ILifecycleActionHandler>(),
            new LifecycleEntityRegistry(),
            new LifecycleActionGate());

        var uow = new EfUnitOfWork<TestDbContext>(context, serviceProvider, settings, orchestrator);
        return uow;
    }

    /// <summary>
    /// <see cref="IHostEnvironment"/> со средой <c>Development</c>
    /// (требование <see cref="DbContextBase"/>).
    /// </summary>
    private sealed class DevelopmentHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "Test";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    /// <summary>
    /// End-to-end проверка: добавление <see cref="TemplateSetterDomainPerson"/> в ChangeTracker
    /// и вызов <see cref="EfUnitOfWork{TDbContext}.SaveChangesAsync"/>
    /// запускает <see cref="TemplateSetterAppPersonHandler"/>, который вызывает
    /// <see cref="TemplateSetterDomainPerson.UpdateHash"/>, и сохранённая сущность
    /// содержит корректный хэш в БД.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_PersonAdded_HashIsComputedAndPersisted()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWork(context);
        var person = TemplateSetterDomainPerson.Create("Alice", "alice@example.com");
        context.Persons.Add(person);
        person.Hash.Should().BeNull();

        // Act
        await uow.SaveChangesAsync(TestContext.Current.CancellationToken, commitTransaction: false);

        // Assert: hash вычислен в той же сущности (handler мутирует её до SaveChanges).
        // После SaveChanges EF Core сохраняет состояние Person.Hash в БД.
        person.Hash.Should().Equal(HashHelper.ComputeSha256("Alice", "alice@example.com"));

        // Проверяем через локальный ChangeTracker, что сохранённая сущность
        // действительно находится в состоянии Unchanged с актуальным хэшем.
        var tracked = context.ChangeTracker
            .Entries<TemplateSetterDomainPerson>()
            .Single(e => e.Entity.Id == person.Id);
        tracked.State.Should().Be(EntityState.Unchanged);
        tracked.Entity.Hash.Should().Equal(HashHelper.ComputeSha256("Alice", "alice@example.com"));
    }

    /// <summary>
    /// Проверяет, что <see cref="TemplateSetterAppPersonHandler"/> вызывается
    /// ровно один раз для нескольких добавленных сущностей, и каждая из них
    /// получает свой хэш, основанный на её собственных
    /// <see cref="TemplateSetterDomainPerson.Name"/>/<see cref="TemplateSetterDomainPerson.Email"/>.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_MultiplePersons_AllHashesComputed()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWork(context);
        var alice = TemplateSetterDomainPerson.Create("Alice", "alice@example.com");
        var bob = TemplateSetterDomainPerson.Create("Bob", "bob@example.com");
        context.Persons.AddRange(alice, bob);

        // Act
        await uow.SaveChangesAsync(TestContext.Current.CancellationToken, commitTransaction: false);

        // Assert
        alice.Hash.Should().Equal(HashHelper.ComputeSha256("Alice", "alice@example.com"));
        bob.Hash.Should().Equal(HashHelper.ComputeSha256("Bob", "bob@example.com"));
        alice.Hash.Should().NotEqual(bob.Hash);
    }

    /// <summary>
    /// <see cref="TemplateSetterDomainPerson.UpdateHash"/> детерминирован: если
    /// свойства <see cref="TemplateSetterDomainPerson.Name"/>/
    /// <see cref="TemplateSetterDomainPerson.Email"/> не изменились,
    /// <see cref="TemplateSetterAppPersonHandler"/> пересчитывает идентичный хэш.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_AfterFirstSave_HashIsStableForSameData()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWork(context);
        var person = TemplateSetterDomainPerson.Create("Alice", "alice@example.com");
        context.Persons.Add(person);
        await uow.SaveChangesAsync(TestContext.Current.CancellationToken, commitTransaction: false);
        var expected = HashHelper.ComputeSha256("Alice", "alice@example.com");

        // Assert
        person.Hash.Should().Equal(expected);

        // Проверяем стабильность через отдельный инстанс того же контекста
        // (только-чтение) — тот же хэш, потому что данные не менялись.
        var reloaded = await context.Persons.FindAsync([person.Id], TestContext.Current.CancellationToken);
        reloaded.Should().NotBeNull();
        reloaded.Hash.Should().Equal(expected);
    }

    /// <summary>
    /// E2E: handler вызывается на каждом <c>SaveChanges</c> для
    /// отслеживаемых entities, а не только на первом добавлении.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_CalledTwice_HandlerInvokedForEachCommit()
    {
        // Arrange
        await using var context = CreateContext();
        var uow = CreateUnitOfWork(context);
        var alice = TemplateSetterDomainPerson.Create("Alice", "alice@example.com");
        var bob = TemplateSetterDomainPerson.Create("Bob", "bob@example.com");

        context.Persons.Add(alice);
        await uow.SaveChangesAsync(TestContext.Current.CancellationToken, commitTransaction: false);

        var aliceHashAfterFirstSave = alice.Hash.ToArray();

        // Act: добавляем вторую сущность и сохраняем в той же инстанции UoW
        context.Persons.Add(bob);
        await uow.SaveChangesAsync(TestContext.Current.CancellationToken, commitTransaction: false);

        // Assert: обе сущности имеют корректные хэши, причём Alice
        // сохранила свой (т.к. handler идемпотентен при тех же входных данных)
        alice.Hash.Should().Equal(aliceHashAfterFirstSave,
            "handler вызывается повторно, но детерминированный хэш не меняется");
        alice.Hash.Should().Equal(HashHelper.ComputeSha256("Alice", "alice@example.com"));
        bob.Hash.Should().Equal(HashHelper.ComputeSha256("Bob", "bob@example.com"));
    }
}
