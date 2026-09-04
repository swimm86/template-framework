// ----------------------------------------------------------------------------------------------
// <copyright file="MapperContextAccessor.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Mapping.Interfaces;

namespace Shared.Infrastructure.Mapper.Mapster.Utils;

/// <summary>
/// AsyncLocal-доступ к текущему активному <see cref="IMapper"/>
/// для использования в кастомных конвертерах типов (type converter), когда Mapster вызывает наш
/// <see cref="ITypeConverter{TSource, TDestination}"/> через адаптер.
/// Расширяет базовый <see cref="Core.Scope.MapperContextAccessor"/> дополнительным
/// свойством <see cref="CurrentMappingTarget"/> для in-place преобразования (mapping).
/// </summary>
internal static class MapperContextAccessor
{
    private static readonly AsyncLocal<object?> _currentMappingTarget = new();

    /// <summary>
    /// Текущий целевой объект, в который выполняется преобразование (mapping). Выставляется в <c>Mapper.Map</c>
    /// перед вызовом Mapster, чтобы wrapper-ы могли получить к нему доступ.
    /// </summary>
    public static object? CurrentMappingTarget
    {
        get => _currentMappingTarget.Value;
        set => _currentMappingTarget.Value = value;
    }
}
