// ----------------------------------------------------------------------------------------------
// <copyright file="MapperContextAccessor.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Mapping.Interfaces;

namespace Shared.Infrastructure.Mapper.Core.Scope;

/// <summary>
/// AsyncLocal-доступ к текущему <see cref="IMapper"/> для использования в кастомных конвертерах типов (type converter),
/// когда провайдер преобразования (mapping) обращается к нашему <see cref="ITypeConverter{TSource, TDestination}"/>.
/// </summary>
internal static class MapperContextAccessor
{
    private static readonly AsyncLocal<IMapper?> _current = new();

    /// <summary>
    /// Текущий активный <see cref="IMapper"/>, выставленный через обёртку
    /// <c>Mapper</c> на время выполнения <c>Map</c>/<c>ProjectTo</c>.
    /// </summary>
    public static IMapper? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}
