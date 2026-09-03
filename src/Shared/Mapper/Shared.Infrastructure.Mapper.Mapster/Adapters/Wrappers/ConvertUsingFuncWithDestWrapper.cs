// ----------------------------------------------------------------------------------------------
// <copyright file="ConvertUsingFuncWithDestWrapper.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Mapping;
using Shared.Infrastructure.Mapper.Mapster.Utils;
using CoreNullMapper = Shared.Infrastructure.Mapper.Core.NullMapper;

namespace Shared.Infrastructure.Mapper.Mapster.Adapters.Wrappers;

/// <summary>
/// Обёртка функции-конвертера (type converter) (src, dest, ctx) для Mapster <c>MapWith</c>.
/// </summary>
/// <typeparam name="TSource">Исходный тип.</typeparam>
/// <typeparam name="TDestination">Целевой тип.</typeparam>
internal sealed class ConvertUsingFuncWithDestWrapper<TSource, TDestination>(
    Func<object, object, ResolutionContext, object> func)
{
    /// <summary>
    /// Преобразует исходный объект через функцию-конвертер с существующим целевым объектом
    /// и контекстом маппинга, возвращая результат конвертации.
    /// </summary>
    /// <remarks>
    /// Поиск фактического <c>destination</c> выполняется в три этапа:
    /// <list type="number">
    ///   <item>Если в <see cref="MapperContextAccessor.CurrentMappingTarget"/> сохранён объект типа <typeparamref name="TDestination"/> (in-place сценарий через <c>Mapper.Map(src, dest)</c>), используется он.</item>
    ///   <item>Иначе используется переданный <paramref name="destination"/>, если он не равен <c>null</c>.</item>
    ///   <item>Иначе создаётся пустая коллекция правильного типа — это защита от подмены <c>List&lt;T&gt;</c> со стороны Mapster.</item>
    /// </list>
    /// </remarks>
    /// <param name="source">Исходный объект.</param>
    /// <param name="destination">Существующий целевой объект; допускается <c>null</c>, в этом случае применяются резервные стратегии поиска.</param>
    /// <returns>Результат конвертации.</returns>
    /// <exception cref="InvalidCastException">Выбрасывается, если приведение к <typeparamref name="TDestination"/> невозможно.</exception>
    public TDestination Invoke(TSource source, TDestination destination)
    {
        // Пробуем получить "настоящий" существующий destination из MapperContextAccessor.CurrentMappingTarget.
        // Это значение выставляется в Mapper.Map(src, dest) перед вызовом Mapster, чтобы
        // корректно работать с in-place маппингом, даже когда Mapster не передаёт dest в MapToTargetWith.
        var storedTarget = MapperContextAccessor.CurrentMappingTarget;
        if (storedTarget is TDestination existing)
        {
            destination = existing;
        }
        else if (destination is null)
        {
            // Mapster при отсутствии dest может создать List<TDest>, что не подходит для нашей логики.
            // Создаём пустую коллекцию правильного типа.
            destination = (TDestination)(object)new List<TDestination>();
        }

        var mapper = Core.Scope.MapperContextAccessor.Current ?? new CoreNullMapper();
        var sharedContext = new ResolutionContext(mapper);
        return (TDestination)func(source!, destination, sharedContext);
    }
}
