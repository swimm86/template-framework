using System.Linq.Expressions;

namespace Shared.Application.Core.Mapping.Interfaces;

/// <summary>
/// Интерфейс маппера.
/// </summary>
public interface IMapper
{
    /// <summary>
    /// Маппинг исходного типа <see cref="TSource"/> в целевой тип <see cref="TResult"/>.
    /// </summary>
    /// <typeparam name="TSource">Исходный тип.</typeparam>
    /// <typeparam name="TResult">Целевой тип.</typeparam>
    /// <param name="source">Экземпляр исходного типа.</param>
    /// <returns>Экземпляр целевого типа.</returns>
    TResult Map<TSource, TResult>(TSource source);

    /// <summary>
    /// Проекция объектов в целевой тип <see cref="TResult"/>.
    /// </summary>
    /// <typeparam name="TResult">Целевой тип.</typeparam>
    /// <param name="source"></param>
    /// <param name="parameters"></param>
    /// <param name="membersToExpand"></param>
    /// <returns></returns>
    IQueryable<TResult> ProjectTo<TResult>(
        IQueryable source,
        object? parameters = null,
        params Expression<Func<TResult, object>>[] membersToExpand
    );
}