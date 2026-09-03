// ----------------------------------------------------------------------------------------------
// <copyright file="MapperContextScope.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Mapping.Interfaces;

namespace Shared.Infrastructure.Mapper.Core.Scope;

/// <summary>
/// Disposable-обёртка для безопасной работы с <see cref="MapperContextAccessor"/>:
/// сохраняет предыдущее значение в конструкторе и гарантированно восстанавливает его в <see cref="Dispose"/>,
/// даже если вложенный код бросил исключение.
/// </summary>
internal readonly struct MapperContextScope
    : IDisposable
{
    private readonly IMapper? _previousMapper;

    /// <summary>
    /// Инициализирует новый scope и выставляет <paramref name="current"/> в качестве активного преобразователя (mapper).
    /// </summary>
    /// <param name="current">Текущий активный <see cref="IMapper"/>.</param>
    public MapperContextScope(IMapper current)
    {
        _previousMapper = MapperContextAccessor.Current;
        MapperContextAccessor.Current = current;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        MapperContextAccessor.Current = _previousMapper;
    }
}
