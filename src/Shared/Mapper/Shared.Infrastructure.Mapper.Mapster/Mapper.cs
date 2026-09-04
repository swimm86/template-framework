// ----------------------------------------------------------------------------------------------
// <copyright file="Mapper.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Mapster;
using Shared.Common.Extensions;
using Shared.Domain.Core.Mapping.Interfaces;
using Shared.Infrastructure.Mapper.Mapster.Utils;
using MapContextScope = Shared.Infrastructure.Mapper.Mapster.Utils.MapContextScope;

namespace Shared.Infrastructure.Mapper.Mapster;

/// <summary>
/// Преобразователь (mapper) на основе Mapster.
/// </summary>
public class Mapper(
    MapsterMapper.IMapper mapper)
    : IMapper
{
    /// <inheritdoc />
    public TDestination Map<TSource, TDestination>(TSource source)
    {
        using var scope = new MapsterMapperContextScope(this);
        return mapper.Map<TSource, TDestination>(source);
    }

    /// <inheritdoc />
    public IQueryable<TDestination> ProjectTo<TDestination>(
        IQueryable source,
        object? parameters = null)
    {
        // Сначала проверяем быстрый путь (тип-идентичный), не трогая контекст.
        var sourceType = source.GetType();
        if (sourceType is { IsGenericType: true, GenericTypeArguments.Length: 1 } &&
            typeof(TDestination) == sourceType.GenericTypeArguments[0])
        {
            return (source as IQueryable<TDestination>)!;
        }

        using var mapperScope = new MapsterMapperContextScope(this);
        using var mapContextScope = MapContextScope.Create();

        var builder = source.BuildAdapter(mapper.Config);

        var parameterValues = GetParameterValues(source.ElementType, typeof(TDestination), parameters);

        foreach (var (key, value) in parameterValues)
        {
            builder = builder.AddParameters(key, value!);
            MapContext.Current!.Parameters[key] = value!;
        }

        return builder.ProjectToType<TDestination>();
    }

    /// <inheritdoc />
    public void Map<TSource, TResult>(TSource source, TResult result)
    {
        using var scope = new MapsterMapperContextScope(this, result!);
        mapper.Map(source, result);
    }

    private static ICollection<KeyValuePair<string, object?>> GetParameterValues(
        Type sourceType,
        Type destType,
        object? parameters)
    {
        var knownKeys = ParameterKeyRegistry.GetKeys(sourceType, destType) ?? [];
        var parameterValues = parameters switch
        {
            null => [],
            IDictionary<string, object?> dict => dict.ToDictionary(),
            _ => parameters.GetType()
                .GetProperties()
                .Select(prop => (prop.Name, prop.GetValue(parameters)))
                .ToDictionary()
        };

        knownKeys.ForEach(key => parameterValues.TryAdd(key, null));

        return parameterValues;
    }
}
