// ----------------------------------------------------------------------------------------------
// <copyright file="ExpressionCombinationBenchmark.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Shared.Common.Extensions;

namespace Shared.Common.Benchmarks;

/// <summary>
/// Сравнение стратегий объединения предикатов на <see cref="IQueryable{T}"/>:
/// попарное (текущая <c>CombineAllAndAlso</c>), линейное (<c>Aggregate(And)</c>),
/// sequential <c>Where()</c> (поведение <c>EfQueryEvaluator</c> до оптимизации).
/// </summary>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ExpressionCombinationBenchmark
{
    private static readonly IQueryable<BenchmarkEntity> QueryableData =
        Enumerable.Range(0, 1000)
            .Select(i => new BenchmarkEntity { Id = i })
            .AsQueryable();

    private List<Expression<Func<BenchmarkEntity, bool>>>? _predicates;

    [Params(1, 2, 8, 32, 128, 512)]
    public int PredicateCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _predicates = Enumerable.Range(0, PredicateCount)
            .Select(i => (Expression<Func<BenchmarkEntity, bool>>)(e => e.Id != i))
            .ToList();
    }

    /// <summary>
    /// Текущая реализация: попарное объединение через <c>CombineAllAndAlso</c>.
    /// </summary>
    [Benchmark(Baseline = true, Description = "Pairwise (CombineAllAndAlso)")]
    public Expression<Func<BenchmarkEntity, bool>>? Pairwise() => _predicates!.CombineAllAndAlso();

    /// <summary>
    /// Альтернативная линейная свёртка через <c>Aggregate(And)</c>.
    /// Демонстрирует исходную логику, приводившую к переполнению стека на больших N.
    /// </summary>
    [Benchmark(Description = "Linear Aggregate")]
    public Expression<Func<BenchmarkEntity, bool>>? LinearAggregate() => _predicates!.Aggregate(ExpressionExtensions.And);

    /// <summary>
    /// Sequential <c>Where()</c> — поведение <c>EfQueryEvaluator</c> до изменения.
    /// Измеряет стоимость построения цепочки из N отдельных <c>IQueryable.Where()</c>.
    /// </summary>
    [Benchmark(Description = "Sequential Where() (original)")]
    public int SequentialWhere()
    {
        IQueryable<BenchmarkEntity> queryable = QueryableData;
        foreach (var predicate in _predicates!)
        {
            queryable = queryable.Where(predicate);
        }
        return queryable.Count();
    }

    /// <summary>
    /// Combined <c>Where()</c> — поведение <c>EfQueryEvaluator</c> с <c>CombineAllAndAlso</c>.
    /// </summary>
    [Benchmark(Description = "Combined Where()")]
    public int CombinedWhere()
    {
        IQueryable<BenchmarkEntity> queryable = QueryableData;
        var combined = _predicates!.CombineAllAndAlso();
        if (combined is not null)
        {
            queryable = queryable.Where(combined);
        }
        return queryable.Count();
    }

    /// <summary>
    /// Проверка: при N=8192 попарный вариант успешно компилируется.
    /// </summary>
    [Benchmark(Description = "Compile pairwise @ N=8192")]
    public object CompilePairwiseLarge()
    {
        var predicates = Enumerable.Range(0, 8192)
            .Select(i => (Expression<Func<BenchmarkEntity, bool>>)(e => e.Id != i))
            .ToList();
        return predicates.CombineAllAndAlso()!.Compile();
    }

    /// <summary>
    /// Проверка: при N=8192 линейный вариант падает с <see cref="StackOverflowException"/>.
    /// </summary>
    [Benchmark(Description = "Compile linear @ N=8192")]
    public object? CompileLinearLarge()
    {
        var predicates = Enumerable.Range(0, 8192)
            .Select(i => (Expression<Func<BenchmarkEntity, bool>>)(e => e.Id != i))
            .ToList();
        try
        {
            return predicates.Aggregate(ExpressionExtensions.And).Compile();
        }
        catch (StackOverflowException)
        {
            return null;
        }
    }

    public sealed class BenchmarkEntity
    {
        public int Id { get; set; }
    }
}