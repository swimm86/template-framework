// ----------------------------------------------------------------------------------------------
// <copyright file="ExceptionMapperResolver.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dto.Responses;
using Shared.Presentation.Core.Exceptions.Interfaces;
using Shared.Presentation.Core.Exceptions.Mappers;

namespace Shared.Presentation.Core.Exceptions;

/// <summary>
/// Резолвер маппера по иерархии типов: обходит <see cref="Exception.GetType"/> и базовые типы,
/// возвращает первый зарегистрированный маппер (самый производный выигрывает).
/// </summary>
internal sealed class ExceptionMapperResolver
    : IExceptionMapperResolver
{
    private readonly Dictionary<Type, IExceptionMapper> _map;

    /// <summary>
    /// Конструктор класса.
    /// </summary>
    /// <param name="mappers">Все зарегистрированные мапперы.</param>
    public ExceptionMapperResolver(
        IEnumerable<IExceptionMapper> mappers)
    {
        _map = CreateMap(mappers);
        if (_map.GetValueOrDefault(typeof(Exception)) == null)
        {
            throw new InvalidOperationException(
                $"{nameof(DefaultExceptionMapper)} ({nameof(IExceptionMapper<Exception>)}) не зарегистрирован");
        }
    }

    /// <inheritdoc />
    public ErrorResponse Map(Exception exception)
    {
        for (var type = exception.GetType(); type is not null; type = type.BaseType)
        {
            if (_map.TryGetValue(type, out var mapper))
            {
                return mapper.Map(exception);
            }
        }

        throw new InvalidOperationException(
            $"Не зарегистрирован маппер для типа {exception.GetType().Name}. " +
            $"Убедитесь, что зарегистрирован {nameof(DefaultExceptionMapper)} ({nameof(IExceptionMapper<Exception>)}).");
    }

    private static Dictionary<Type, IExceptionMapper> CreateMap(
        IEnumerable<IExceptionMapper> mappers)
    {
        var result = new Dictionary<Type, IExceptionMapper>();
        foreach (var mapper in mappers)
        {
            if (!result.TryAdd(mapper.HandledType, mapper))
            {
                throw new InvalidOperationException(
                    $"Для типа {mapper.HandledType.Name} зарегистрировано несколько мапперов: " +
                    $"{result[mapper.HandledType].GetType().Name} и {mapper.GetType().Name}.");
            }
        }

        return result;
    }
}
