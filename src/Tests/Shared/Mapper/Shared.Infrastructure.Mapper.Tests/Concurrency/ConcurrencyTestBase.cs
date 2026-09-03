// ----------------------------------------------------------------------------------------------
// <copyright file="ConcurrencyTestBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using FluentAssertions;
using Shared.Domain.Core.Interfaces;
using Shared.Domain.Core.Mapping;
using Shared.Domain.Core.Mapping.Extensions;
using Shared.Domain.Core.Mapping.Interfaces;
using Xunit;

namespace Shared.Infrastructure.Mapper.Tests.Concurrency;

/// <summary>
/// Базовый класс тестов thread-safety маппера. Содержит общие тесты
/// параллельных сценариев (<see cref="ConfigureCollection_Parallel_KeepsDestinationIsolated"/>,
/// <see cref="Mapper_Parallel_DoesNotMixDestinations"/>). Наследник реализует
/// <see cref="CreateMapper"/> для конкретного провайдера.
/// </summary>
public abstract class ConcurrencyTestBase
{
    [Fact]
    public async Task ConfigureCollection_Parallel_KeepsDestinationIsolated()
    {
        // Arrange
        var mapper = CreateMapper();

        var batches = Enumerable.Range(0, 100)
            .Select(i => (Index: i, A: Guid.NewGuid(), B: Guid.NewGuid()))
            .ToList();

        // Act
        var tasks = batches.Select(async batch =>
        {
            var source = new List<Item>
            {
                new() { Id = batch.A, Name = $"A-{batch.Index}" },
                new() { Id = batch.B, Name = $"B-{batch.Index}-updated" },
            };

            var destination = new List<ItemDest>
            {
                new() { Id = batch.A, Name = $"A-{batch.Index}-old" },
            };

            mapper.Map<ICollection<Item>, ICollection<ItemDest>>(source, destination);

            return (batch, destination);
        }).ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(100);
        foreach (var (batch, dest) in results)
        {
            dest.Should().HaveCount(2);
            dest.Single(x => x.Id == batch.A).Name.Should().Be($"A-{batch.Index}");
            dest.Single(x => x.Id == batch.B).Name.Should().Be($"B-{batch.Index}-updated");
        }
    }

    [Fact]
    public async Task Mapper_Parallel_DoesNotMixDestinations()
    {
        // Arrange
        var mapper = CreateMapper();
        var aId = Guid.NewGuid();
        var bId = Guid.NewGuid();

        // Act
        var task1 = Task.Run(() => mapper.Map<Item, ItemDest>(new Item { Id = aId, Name = "A" }));
        var task2 = Task.Run(() => mapper.Map<Item, ItemDest>(new Item { Id = bId, Name = "B" }));
        var results = await Task.WhenAll(task1, task2);

        // Assert
        results[0].Id.Should().Be(aId);
        results[0].Name.Should().Be("A");
        results[1].Id.Should().Be(bId);
        results[1].Name.Should().Be("B");
    }

    /// <summary>
    /// Создаёт экземпляр <see cref="IMapper"/> для тестируемого провайдера.
    /// </summary>
    /// <returns>Готовый к использованию <see cref="IMapper"/>.</returns>
    protected abstract IMapper CreateMapper();

    /// <summary>
    /// Исходный тип.
    /// </summary>
    protected sealed class Item : IEntity<Guid>
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Целевой тип.
    /// </summary>
    protected sealed class ItemDest : IEntity<Guid>
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Профиль маппинга.
    /// </summary>
    protected sealed class Profile : MappingProfileBase
    {
        public Profile()
        {
            CreateMap<Item, ItemDest>();
            CreateMap<ICollection<Item>, ICollection<ItemDest>>().ConfigureCollection();
        }
    }
}
