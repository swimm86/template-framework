// ----------------------------------------------------------------------------------------------
// <copyright file="TemplateSetterAppPersonHandlerTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.LifecycleAction.Interfaces;
using Shared.Common.Helpers;
using Shared.Domain.Core.Enums;
using TemplateSetterAppPersonHandler = Template.Setter.Application.LifecycleAction.Person.PersonHashLifecycleHandler;
using TemplateSetterDomainPerson = Template.Domain.Entities.Person;

namespace Template.Setter.Application.Tests.LifecycleAction.Person;

/// <summary>
/// Тесты для <see cref="TemplateSetterAppPersonHandler"/>.
/// Проверяют, что handler вызывается только в фазе <see cref="LifecyclePhase.BeforeSave"/>,
/// только для сущностей типа <see cref="TemplateSetterDomainPerson"/>, и обновляет хэш
/// по правилам <see cref="HashHelper.ComputeSha256"/>.
/// </summary>
public sealed class TemplateSetterAppPersonHandlerTests
{
    /// <summary>
    /// Handler объявляет фазу <see cref="LifecyclePhase.BeforeSave"/> —
    /// диспетчеризация AfterSave не должна его вызывать.
    /// </summary>
    [Fact]
    public void Phase_IsBeforeSave()
    {
        // Arrange
        var handler = new TemplateSetterAppPersonHandler();

        // Act
        var phase = handler.Phase;

        // Assert
        phase.Should().Be(LifecyclePhase.BeforeSave);
    }

    /// <summary>
    /// Handler объявляет уникальный стабильный ключ для отключения/включения
    /// через <c>orchestrator.DisableActions(string[])</c>.
    /// </summary>
    [Fact]
    public void Key_IsStableAndNonEmpty()
    {
        // Arrange
        var handler = new TemplateSetterAppPersonHandler();

        // Act
        var key = handler.Key;

        // Assert
        key.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Handler заявляет, что работает с сущностями
    /// <see cref="TemplateSetterDomainPerson"/>.
    /// </summary>
    [Fact]
    public void EntityType_IsPerson()
    {
        // Arrange
        ILifecycleActionHandler handler = new TemplateSetterAppPersonHandler();

        // Act
        var type = handler.EntityType;

        // Assert
        type.Should().Be<TemplateSetterDomainPerson>();
    }

    /// <summary>
    /// <see cref="TemplateSetterAppPersonHandler"/> вызывает
    /// <see cref="TemplateSetterDomainPerson.UpdateHash"/> для каждой переданной сущности,
    /// и после выполнения хэш соответствует <see cref="HashHelper.ComputeSha256"/>
    /// от <c>(Name, Email)</c>.
    /// </summary>
    [Fact]
    public async Task ExecuteActionAsync_SetsHashFromNameAndEmail()
    {
        // Arrange
        var person = TemplateSetterDomainPerson.Create("Alice", "alice@example.com");
        var handler = new TemplateSetterAppPersonHandler();

        // Act
        await ((ILifecycleActionHandler<TemplateSetterDomainPerson>)handler).ExecuteAsync(
            [person],
            CancellationToken.None);

        // Assert
        person.Hash.Should().Equal(HashHelper.ComputeSha256("Alice", "alice@example.com"));
    }

    /// <summary>
    /// Обработчик корректно обрабатывает коллекцию из нескольких сущностей —
    /// у каждой пересчитывается свой хэш.
    /// </summary>
    [Fact]
    public async Task ExecuteActionAsync_ForEachEntity_RecomputesHash()
    {
        // Arrange
        var alice = TemplateSetterDomainPerson.Create("Alice", "alice@example.com");
        var bob = TemplateSetterDomainPerson.Create("Bob", "bob@example.com");
        var handler = new TemplateSetterAppPersonHandler();

        // Act
        await ((ILifecycleActionHandler<TemplateSetterDomainPerson>)handler).ExecuteAsync(
            [alice, bob],
            CancellationToken.None);

        // Assert
        alice.Hash.Should().Equal(HashHelper.ComputeSha256("Alice", "alice@example.com"));
        bob.Hash.Should().Equal(HashHelper.ComputeSha256("Bob", "bob@example.com"));
    }

    /// <summary>
    /// Если в коллекции нет сущностей, handler не выполняет полезной работы
    /// (no-op) и не бросает исключений.
    /// </summary>
    [Fact]
    public async Task ExecuteActionAsync_EmptyCollection_NoOp()
    {
        // Arrange
        var handler = new TemplateSetterAppPersonHandler();

        // Act
        var act = () => ((ILifecycleActionHandler<TemplateSetterDomainPerson>)handler)
            .ExecuteAsync([], CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
