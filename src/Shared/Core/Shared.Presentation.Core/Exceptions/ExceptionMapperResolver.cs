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
/// Преобразователь исключений по иерархии типов: обходит <see cref="Exception.GetType"/> и базовые типы,
/// возвращает первый зарегистрированный преобразователь (самый производный выигрывает).
/// </summary>
internal sealed class ExceptionMapperResolver
    : IExceptionMapperResolver
{
    private readonly Dictionary<Type, IExceptionMapper> _map;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="ExceptionMapperResolver"/>.
    /// </summary>
    /// <param name="mappers">Все зарегистрированные преобразователи исключений.</param>
    public ExceptionMapperResolver(
        IEnumerable<IExceptionMapper> mappers)
    {
        _map = CreateMap(mappers);
        if (_map.GetValueOrDefault(typeof(Exception)) == null)
        {
            throw new InvalidOperationException(
                $"{nameof(DefaultExceptionMapper)} ({nameof(IExceptionMapper<Exception>)}) is not registered.");
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
            $"No exception mapper is registered for type {exception.GetType().Name}. " +
            $"Ensure that {nameof(DefaultExceptionMapper)} ({nameof(IExceptionMapper<Exception>)}) is registered.");
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
                    $"Multiple exception mappers are registered for type {mapper.HandledType.Name}: " +
                    $"{result[mapper.HandledType].GetType().Name} and {mapper.GetType().Name}.");
            }
        }

        return result;
    }
}
