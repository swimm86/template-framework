// ----------------------------------------------------------------------------------------------
// <copyright file="MapsterMapperContextScope.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Mapping.Interfaces;

namespace Shared.Infrastructure.Mapper.Mapster.Utils;

/// <summary>
/// Disposable-обёртка для безопасной работы с <see cref="Core.Scope.MapperContextAccessor"/>
/// и <see cref="Utils.MapperContextAccessor"/>: сохраняет предыдущие значения в конструкторе и
/// гарантированно восстанавливает их в <see cref="Dispose"/>, даже если вложенный код бросил исключение.
/// Использует композицию с <see cref="Core.Scope.MapperContextScope"/> для
/// управления базовым свойством <c>Current</c> без дублирования логики.
/// </summary>
internal readonly struct MapsterMapperContextScope
    : IDisposable
{
    private readonly Core.Scope.MapperContextScope _baseScope;
    private readonly object? _previousTarget;
    private readonly bool _setTarget;

    /// <summary>
    /// Инициализирует новый scope и выставляет <paramref name="current"/> в качестве активного преобразователя (mapper).
    /// Делегирует работу с <see cref="Core.Scope.MapperContextAccessor.Current"/>
    /// базовому <see cref="Core.Scope.MapperContextScope"/>.
    /// </summary>
    /// <param name="current">Текущий активный <see cref="IMapper"/>.</param>
    public MapsterMapperContextScope(IMapper current)
    {
        _baseScope = new Core.Scope.MapperContextScope(current);
        _previousTarget = null;
        _setTarget = false;
    }

    /// <summary>
    /// Инициализирует новый scope, выставляет активный преобразователь (mapper) и сохраняет целевой объект
    /// для in-place преобразования (mapping).
    /// </summary>
    /// <param name="current">Текущий активный <see cref="IMapper"/>.</param>
    /// <param name="mappingTarget">Целевой объект, в который выполняется преобразование.</param>
    public MapsterMapperContextScope(IMapper current, object mappingTarget)
    {
        _baseScope = new Core.Scope.MapperContextScope(current);
        _previousTarget = MapperContextAccessor.CurrentMappingTarget;
        _setTarget = true;
        MapperContextAccessor.CurrentMappingTarget = mappingTarget;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _baseScope.Dispose();
        if (_setTarget)
        {
            MapperContextAccessor.CurrentMappingTarget = _previousTarget;
        }
    }
}
