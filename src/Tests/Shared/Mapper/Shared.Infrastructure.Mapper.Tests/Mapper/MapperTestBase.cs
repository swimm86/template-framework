// ----------------------------------------------------------------------------------------------
// <copyright file="MapperTestBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using FluentAssertions;
using Shared.Domain.Core.Mapping;
using Shared.Domain.Core.Mapping.Interfaces;
using Xunit;

namespace Shared.Infrastructure.Mapper.Tests.Mapper;

/// <summary>
/// Базовый класс базовых тестов маппера. Содержит общие тесты.
/// Наследник реализует <see cref="CreateMapper"/>, который возвращает <see cref="IMapper"/> для конкретного провайдера.
/// </summary>
/// <remarks>
/// Класс намеренно содержит DTO/Source типы — они общие для всех реализаций (AutoMapper, Mapster и т.п.).
/// </remarks>
public abstract class MapperTestBase
{
    /// <summary>
    /// Проверяет, что <see cref="IMapper.Map{TSource, TDestination}(TSource)"/> корректно
    /// делегирует маппинг простых типов.
    /// </summary>
    [Fact]
    public void Map_SimpleTypes()
    {
        // Arrange
        var mapper = CreateMapper();

        // Act
        var result = mapper.Map<Source, Destination>(new Source { Id = 1, Name = "Test" });

        // Assert
        result.Id.Should().Be(1);
        result.Name.Should().Be("Test");
    }

    /// <summary>
    /// Проверяет, что <see cref="IMapper.Map{TSource, TDestination}(TSource, TDestination)"/>
    /// корректно маппит источник в существующий целевой объект.
    /// </summary>
    [Fact]
    public void Map_InPlace()
    {
        // Arrange
        var mapper = CreateMapper();
        var source = new Source { Id = 2, Name = "Updated" };
        var destination = new Destination { Id = 0, Name = "Original" };

        // Act
        mapper.Map(source, destination);

        // Assert
        destination.Id.Should().Be(2);
        destination.Name.Should().Be("Updated");
        source.Id.Should().Be(2);
        source.Name.Should().Be("Updated");
    }

    /// <summary>
    /// Проверяет, что <see cref="IMapper.Map{TSource, TDestination}(TSource)"/> корректно
    /// работает для обратного маппинга, зарегистрированного через <c>CreateMap</c>.
    /// </summary>
    [Fact]
    public void Map_ReverseDirection()
    {
        // Arrange
        var mapper = CreateMapper();

        // Act
        var result = mapper.Map<Destination, Source>(new Destination { Id = 5, Name = "Reverse" });

        // Assert
        result.Id.Should().Be(5);
        result.Name.Should().Be("Reverse");
    }

    /// <summary>
    /// Проверяет, что <see cref="IMapper.Map{TSource, TDestination}(TSource, TDestination)"/>
    /// корректно маппит источник в существующий целевой объект
    /// для обратного маппинга, зарегистрированного через <c>CreateMap</c>.
    /// </summary>
    [Fact]
    public void Map_ReverseDirection_InPlace()
    {
        // Arrange
        var mapper = CreateMapper();
        var source = new Source { Id = 2, Name = "Updated" };
        var destination = new Destination { Id = 0, Name = "Original" };

        // Act
        mapper.Map(destination, source);

        // Assert
        source.Id.Should().Be(0);
        source.Name.Should().Be("Original");
        destination.Id.Should().Be(0);
        destination.Name.Should().Be("Original");
    }

    /// <summary>
    /// Проверяет, что <see cref="IMapper.ProjectTo{TResult}(IQueryable, object?)"/> корректно
    /// проецирует коллекцию.
    /// </summary>
    [Fact]
    public void ProjectTo()
    {
        // Arrange
        var mapper = CreateMapper();
        var sources = new[]
        {
            new Source { Id = 1, Name = "A" },
            new Source { Id = 2, Name = "B" },
        }.AsQueryable();

        // Act
        var result = mapper.ProjectTo<Destination>(sources).ToArray();

        // Assert
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(1);
        result[0].Name.Should().Be("A");
        result[1].Id.Should().Be(2);
        result[1].Name.Should().Be("B");
    }

    /// <summary>
    /// Создаёт экземпляр <see cref="IMapper"/> для тестируемого провайдера.
    /// </summary>
    /// <returns>Готовый к использованию <see cref="IMapper"/>.</returns>
    protected abstract IMapper CreateMapper();

    /// <summary>
    /// Исходный тип.
    /// </summary>
    protected sealed class Source
    {
        public int Id { get; init; }

        public string Name { get; init; } = string.Empty;
    }

    /// <summary>
    /// Целевой тип.
    /// </summary>
    protected sealed class Destination
    {
        public int Id { get; init; }

        public string Name { get; init; } = string.Empty;
    }

    /// <summary>
    /// Тестовый профиль, регистрирующий прямой и обратный маппинги.
    /// </summary>
    protected sealed class TestProfile : MappingProfileBase
    {
        public TestProfile()
        {
            CreateMap<Source, Destination>();
            CreateMap<Destination, Source>();
        }
    }
}
