// ----------------------------------------------------------------------------------------------
// <copyright file="MapContextScope.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Mapster;

namespace Shared.Infrastructure.Mapper.Mapster.Utils;

/// <summary>
/// Disposable-обёртка для безопасной работы с <see cref="MapContext.Current"/>.
/// Сохраняет предыдущий контекст, выставляет свежий и гарантированно восстанавливает в <see cref="Dispose"/>.
/// </summary>
internal readonly struct MapContextScope
    : IDisposable
{
    private readonly MapContext? _previous;

    /// <summary>
    /// Создаёт новый scope, который выставляет чистый <see cref="MapContext"/>.
    /// </summary>
    /// <returns>Инициализированный scope, готовый к использованию в <c>using</c>.</returns>
    public static MapContextScope Create()
    {
        return new MapContextScope(MapContext.Current);
    }

    private MapContextScope(MapContext? previous)
    {
        _previous = previous;
        MapContext.Current = new MapContext();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        MapContext.Current = _previous;
    }
}
