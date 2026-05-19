using System.Linq.Expressions;
using Shared.Domain.Core.Mapping.Interfaces;

namespace Shared.Testing.Doubles.Mapping;

public sealed class FakeMapper : IMapper
{
    private readonly Dictionary<(Type Src, Type Dst), object> _mappers = new();

    public int MapCallCount { get; private set; }
    public int ProjectToCallCount { get; private set; }
    public int MapInPlaceCallCount { get; private set; }

    public void RegisterMap<TSource, TResult>(Func<TSource, TResult> mapper)
        => _mappers[(typeof(TSource), typeof(TResult))] = mapper;

    public TResult Map<TSource, TResult>(TSource source)
    {
        MapCallCount++;
        if (_mappers.TryGetValue((typeof(TSource), typeof(TResult)), out var mapper))
            return ((Func<TSource, TResult>)mapper)(source);
        throw new InvalidOperationException($"No mapping: {typeof(TSource).Name} → {typeof(TResult).Name}");
    }

    public IQueryable<TResult> ProjectTo<TResult>(IQueryable source, object? parameters = null, params Expression<Func<TResult, object>>[] membersToExpand)
    {
        ProjectToCallCount++;
        return source.Cast<TResult>();
    }

    public void Map<TSource, TResult>(TSource source, TResult result)
    {
        MapInPlaceCallCount++;
    }
}
