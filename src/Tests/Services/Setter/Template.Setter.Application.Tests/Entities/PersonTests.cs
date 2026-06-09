// ----------------------------------------------------------------------------------------------
// <copyright file="PersonTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Common.Helpers;
using Template.Domain.Entities;
using TemplateSetterDomainPerson = Template.Domain.Entities.Person;

namespace Template.Setter.Application.Tests.Entities;

/// <summary>
/// Тесты для доменной сущности <see cref="TemplateSetterDomainPerson"/>.
/// Проверяют фабричный метод, инварианты и детерминированность
/// <see cref="TemplateSetterDomainPerson.UpdateHash"/>.
/// </summary>
public sealed class PersonTests
{
    /// <summary>
    /// <see cref="TemplateSetterDomainPerson.Create"/> инициализирует
    /// идентификатор <c>Id</c> уникальным значением (не <see cref="Guid.Empty"/>).
    /// </summary>
    [Fact]
    public void Create_AssignsNonEmptyId()
    {
        // Act
        var person = TemplateSetterDomainPerson.Create("Alice", "alice@example.com");

        // Assert
        person.Id.Should().NotBe(Guid.Empty);
    }

    /// <summary>
    /// <see cref="TemplateSetterDomainPerson.Create"/> сохраняет переданные
    /// имя и email.
    /// </summary>
    [Fact]
    public void Create_PreservesNameAndEmail()
    {
        // Act
        var person = TemplateSetterDomainPerson.Create("Alice", "alice@example.com");

        // Assert
        person.Name.Should().Be("Alice");
        person.Email.Should().Be("alice@example.com");
    }

    /// <summary>
    /// Два вызова <see cref="TemplateSetterDomainPerson.Create"/> с одинаковыми
    /// параметрами дают разные идентификаторы <c>Id</c> (идентификатор уникален),
    /// но одинаковые значения <see cref="TemplateSetterDomainPerson.Name"/>/
    /// <see cref="TemplateSetterDomainPerson.Email"/>.
    /// </summary>
    [Fact]
    public void Create_SameArgs_ProducesDifferentIds()
    {
        // Act
        var first = TemplateSetterDomainPerson.Create("Alice", "alice@example.com");
        var second = TemplateSetterDomainPerson.Create("Alice", "alice@example.com");

        // Assert
        first.Id.Should().NotBe(second.Id);
        first.Name.Should().Be(second.Name);
        first.Email.Should().Be(second.Email);
    }

    /// <summary>
    /// <see cref="TemplateSetterDomainPerson.UpdateHash"/> вычисляет хэш,
    /// совпадающий с <see cref="HashHelper.ComputeSha256"/>(<see cref="TemplateSetterDomainPerson.Name"/>,
    /// <see cref="TemplateSetterDomainPerson.Email"/>).
    /// </summary>
    [Fact]
    public void UpdateHash_SetsHashFromNameAndEmail()
    {
        // Arrange
        var person = TemplateSetterDomainPerson.Create("Alice", "alice@example.com");

        // Act
        person.UpdateHash();

        // Assert
        person.Hash.Should().Equal(HashHelper.ComputeSha256("Alice", "alice@example.com"));
    }

    /// <summary>
    /// <see cref="TemplateSetterDomainPerson.UpdateHash"/> детерминирован:
    /// повторный вызов с теми же значениями <see cref="TemplateSetterDomainPerson.Name"/>/
    /// <see cref="TemplateSetterDomainPerson.Email"/> даёт идентичный массив байт.
    /// </summary>
    [Fact]
    public void UpdateHash_IsDeterministic()
    {
        // Arrange
        var person = TemplateSetterDomainPerson.Create("Alice", "alice@example.com");

        // Act
        person.UpdateHash();
        var first = person.Hash;
        person.UpdateHash();
        var second = person.Hash;

        // Assert
        second.Should().Equal(first);
    }

    /// <summary>
    /// Регистронезависимость: разный регистр имени/email даёт идентичный хэш
    /// после нормализации (<see cref="HashHelper"/> применяет
    /// <c>ToLowerInvariant</c>).
    /// </summary>
    [Fact]
    public void UpdateHash_CaseInsensitiveNormalization()
    {
        // Arrange
        var lower = TemplateSetterDomainPerson.Create("alice", "alice@example.com");
        var upper = TemplateSetterDomainPerson.Create("ALICE", "ALICE@EXAMPLE.COM");

        // Act
        lower.UpdateHash();
        upper.UpdateHash();

        // Assert
        lower.Hash.Should().Equal(upper.Hash,
            "HashHelper нормализует через ToLowerInvariant — разный регистр даёт одинаковый хэш");
    }

    /// <summary>
    /// Тримминг пробелов: <c>"  Alice  "</c> и <c>"Alice"</c> дают
    /// идентичный хэш после нормализации.
    /// </summary>
    [Fact]
    public void UpdateHash_TrimsWhitespace()
    {
        // Arrange
        var withSpaces = TemplateSetterDomainPerson.Create("  Alice  ", "  alice@example.com  ");
        var clean = TemplateSetterDomainPerson.Create("Alice", "alice@example.com");

        // Act
        withSpaces.UpdateHash();
        clean.UpdateHash();

        // Assert
        withSpaces.Hash.Should().Equal(clean.Hash,
            "HashHelper применяет Trim() перед хэшированием");
    }
}
