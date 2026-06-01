// ----------------------------------------------------------------------------------------------
// <copyright file="ProjectSmokeTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Template.Setter.Application.Tests.Smoke;

/// <summary>
/// Smoke-тест инфраструктуры тестового проекта <c>Template.Setter.Application.Tests</c>.
/// </summary>
/// <remarks>
/// Удалить после появления реальных тестов для <see cref="Template.Setter.Application.Features.Person.Validators.PersonValidator"/>.
/// </remarks>
public sealed class ProjectSmokeTests
{
    /// <summary>
    /// Проверяет, что тестовый проект корректно инициализирован и xUnit-v3 работает.
    /// </summary>
    [Fact]
    public void Smoke_TestProjectIsConfigured_AlwaysPasses()
    {
        // Arrange
        var expected = 1;

        // Act
        var actual = 1;

        // Assert
        actual.Should().Be(expected);
    }
}
