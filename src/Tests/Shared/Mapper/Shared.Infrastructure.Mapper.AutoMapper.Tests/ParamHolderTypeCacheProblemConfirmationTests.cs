// ----------------------------------------------------------------------------------------------
// <copyright file="ParamHolderTypeCacheProblemConfirmationTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Reflection;
using Shared.Infrastructure.Mapper.AutoMapper.Adapters.ExpressionTransformers;

namespace Shared.Infrastructure.Mapper.AutoMapper.Tests;

/// <summary>
/// Тесты, объективно подтверждающие проблемы <see cref="ParamHolderTypeCache"/>:
/// <list type="number">
///   <item>Отсутствует API очистки кеша — динамические типы накапливаются неограниченно.</item>
///   <item>Динамически созданные типы удерживаются корневой сборкой навсегда (не собираются GC).</item>
/// </list>
/// <para>
/// Тесты намеренно ожидают сценарий, который должен быть исправлен:
/// сейчас они <b>падают</b> — что и подтверждает наличие проблемы в текущей реализации.
/// </para>
/// </summary>
public sealed class ParamHolderTypeCacheProblemConfirmationTests
{
    [Fact]
    public void Cache_LacksEvictionApi_StaticClearOrResetMethodIsMissing()
    {
        var cacheType = typeof(ParamHolderTypeCache);
        var evictionMethods = cacheType
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic)
            .Where(m => m.Name is "Clear" or "Reset" or "Evict" or "Dispose")
            .ToArray();

        evictionMethods.Should().NotBeEmpty(
            "отсутствует API для очистки/сброса кеша динамических типов — это приводит к unbounded memory growth");
    }

    [Fact]
    public void DynamicHolderType_IsNotGarbageCollected_AfterCreation()
    {
        var keys = new HashSet<string> { $"unique_{Guid.NewGuid():N}" };
        WeakReference<Type>? weakRef = null;

        CreateHolderInIsolatedScope(keys, t => weakRef = new WeakReference<Type>(t));

        for (var i = 0; i < 5; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        var stillAlive = weakRef is not null && weakRef.TryGetTarget(out _);
        stillAlive.Should().BeFalse(
            "динамически сгенерированный holder-тип должен собираться GC после выхода из локальной области; "
            + "текущая реализация удерживает ссылку через статический ModuleBuilder, что подтверждает memory leak");
    }

    [Fact]
    public void DistinctKeySets_ProduceDistinctTypes_NoDeduplication()
    {
        var firstKeys = new HashSet<string> { "a", "b" };
        var secondKeys = new HashSet<string> { "c", "d" };

        var firstType = ParamHolderTypeCache.GetOrCreate(firstKeys);
        var secondType = ParamHolderTypeCache.GetOrCreate(secondKeys);

        firstType.Should().NotBeSameAs(secondType);

        var sameFirstType = ParamHolderTypeCache.GetOrCreate(firstKeys);
        sameFirstType.Should().BeSameAs(firstType);
    }

    private static void CreateHolderInIsolatedScope(HashSet<string> keys, Action<Type> capture)
    {
        var type = ParamHolderTypeCache.GetOrCreate(keys);
        capture(type);
    }
}
