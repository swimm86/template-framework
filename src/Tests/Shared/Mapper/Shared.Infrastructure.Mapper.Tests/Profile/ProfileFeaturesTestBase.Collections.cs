// ----------------------------------------------------------------------------------------------
// <copyright file="ProfileFeaturesTestBase.Collections.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Interfaces;
using Shared.Domain.Core.Mapping;
using Shared.Domain.Core.Mapping.Extensions;

namespace Shared.Infrastructure.Mapper.Tests.Profile;

/// <summary>
/// Тесты <c>ConfigureCollection</c>, вложенных объектов и одновременной регистрации нескольких профилей.
/// </summary>
public abstract partial class ProfileFeaturesTestBase
{
    // ---- 11. ConfigureCollection ----

    private sealed class CollectionItem
        : IEntity<Guid>
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    private sealed class CollectionItemDest
        : IEntity<Guid>
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    private sealed class CollectionProfile
        : MappingProfileBase
    {
        public CollectionProfile()
        {
            CreateMap<CollectionItem, CollectionItemDest>();
            CreateMap<ICollection<CollectionItem>, ICollection<CollectionItemDest>>()
                .ConfigureCollection();
        }
    }

    /// <summary>
    /// <c>ConfigureCollection</c> добавляет новые, обновляет существующие и удаляет отсутствующие элементы коллекции.
    /// </summary>
    [Fact]
    public void ConfigureCollection_AddsUpdatesAndRemovesItems()
    {
        var mapper = CreateMapper();

        var aId = Guid.NewGuid();
        var bId = Guid.NewGuid();
        var cId = Guid.NewGuid();
        var dId = Guid.NewGuid();

        var source = new List<CollectionItem>
        {
            new() { Id = aId, Name = "A" },
            new() { Id = bId, Name = "B-updated" },
            new() { Id = dId, Name = "D-new" },
        };

        var destA = new CollectionItemDest { Id = aId, Name = "A" };
        var destB = new CollectionItemDest { Id = bId, Name = "B-original" };
        var destC = new CollectionItemDest { Id = cId, Name = "C-to-remove" };

        var destination = new List<CollectionItemDest>
        {
            destA,
            destB,
            destC,
        };

        var result = destination;
        mapper.Map<ICollection<CollectionItem>, ICollection<CollectionItemDest>>(source, destination);

        result.Should().HaveCount(3);
        result.Should().NotContain(x => x == destC);

        var resultA = result.SingleOrDefault(x => x.Id == aId);
        var resultB = result.SingleOrDefault(x => x.Id == bId);
        var resultD = result.SingleOrDefault(x => x.Id == dId);

        resultA.Should().NotBeNull();
        resultA.Should().BeSameAs(destA);
        resultA.Name.Should().Be("A");

        resultB.Should().NotBeNull();
        resultB.Should().BeSameAs(destB);
        resultB.Name.Should().Be("B-updated");

        resultD.Should().NotBeNull();
        resultD.Name.Should().Be("D-new");
    }

    /// <summary>
    /// <c>Map(src, dest)</c> копирует скалярные свойства в существующий экземпляр назначения.
    /// </summary>
    [Fact]
    public void Map_InPlace_CopiesScalarProperties()
    {
        // Проверяем базовый механизм Map(src, dest) — должен копировать поля в существующий dest.
        var mapper = CreateMapper();
        var bId = Guid.NewGuid();
        var source = new CollectionItem { Id = bId, Name = "B-updated" };
        var dest = new CollectionItemDest { Id = bId, Name = "B-original" };

        mapper.Map(source, dest);

        dest.Name.Should().Be("B-updated");
    }

    // ---- 12. Несколько профилей одновременно ----

    private sealed class MultiASource
    {
        public int Id { get; set; }
    }

    private sealed class MultiADest
    {
        public int Id { get; set; }
    }

    private sealed class MultiBSource
    {
        public string Text { get; set; } = string.Empty;
    }

    private sealed class MultiBDest
    {
        public string Text { get; set; } = string.Empty;
    }

    private sealed class MultiProfileA
        : MappingProfileBase
    {
        public MultiProfileA()
        {
            CreateMap<MultiASource, MultiADest>();
        }
    }

    private sealed class MultiProfileB
        : MappingProfileBase
    {
        public MultiProfileB()
        {
            CreateMap<MultiBSource, MultiBDest>();
        }
    }

    /// <summary>
    /// Все зарегистрированные профили участвуют в формировании преобразования (mapping).
    /// </summary>
    [Fact]
    public void MultipleProfiles_AllMappingsAreRegistered()
    {
        var mapper = CreateMapper();

        // Trigger MultiProfileA and MultiProfileB instantiation via DI assembly scan
        var aResult = mapper.Map<MultiASource, MultiADest>(new MultiASource { Id = 42 });
        var bResult = mapper.Map<MultiBSource, MultiBDest>(new MultiBSource { Text = "Hello" });

        aResult.Id.Should().Be(42);
        bResult.Text.Should().Be("Hello");
    }

    // ---- 13. Map in-place с вложенными объектами ----

    private sealed class NestedSource
    {
        public string Tag { get; set; } = string.Empty;
    }

    private sealed class NestedDest
    {
        public string Tag { get; set; } = string.Empty;
    }

    private sealed class ParentSource
    {
        public int Id { get; set; }

        public NestedSource Nested { get; set; } = new();
    }

    private sealed class ParentDest
    {
        public int Id { get; set; }

        public NestedDest Nested { get; set; } = new();
    }

    private sealed class NestedProfile
        : MappingProfileBase
    {
        public NestedProfile()
        {
            CreateMap<NestedSource, NestedDest>();
            CreateMap<ParentSource, ParentDest>();
        }
    }

    /// <summary>
    /// Вложенные объекты мапятся рекурсивно по цепочке профилей.
    /// </summary>
    [Fact]
    public void NestedObjects_AreMappedRecursively()
    {
        var mapper = CreateMapper();

        var result = mapper.Map<ParentSource, ParentDest>(
            new ParentSource { Id = 1, Nested = new NestedSource { Tag = "Tag1" } });

        result.Id.Should().Be(1);
        result.Nested.Tag.Should().Be("Tag1");
    }
}
