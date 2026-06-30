// ----------------------------------------------------------------------------------------------
// <copyright file="ISetterRepository.Update.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Dal.Specification.Interfaces;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Dal.Repository.Interfaces;

/// <inheritdoc cref="ISetterRepository{TEntity}"/>
public partial interface ISetterRepository<TEntity>
    where TEntity : class, IEntity
{
    /// <summary>
    /// Выполняет пакетное обновление свойств сущностей на уровне БД (без предварительной загрузки экземпляров) с учётом настроек запроса.
    /// </summary>
    /// <param name="options">Настройки запроса. Если <see langword="null"/>, запрос выполняется без дополнительных настроек.</param>
    /// <param name="updateData">Массив кортежей, содержащий выражения для обновляемых свойств и их значений.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task ExecuteUpdateRangeAsync(
        QueryOptions<TEntity> options,
        params (LambdaExpression propertyExpression, LambdaExpression valueExpression)[] updateData);

    /// <summary>
    /// Выполняет пакетное обновление свойств сущностей на уровне БД (без предварительной загрузки экземпляров) с учётом настроек запроса.
    /// </summary>
    /// <param name="specification">Спецификация.</param>
    /// <param name="updateData">Массив кортежей, содержащий выражения для обновляемых свойств и их значений.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task ExecuteUpdateRangeAsync(
        ISpecification<TEntity> specification,
        params (LambdaExpression propertyExpression, LambdaExpression valueExpression)[] updateData) =>
        ExecuteUpdateRangeAsync(specification.BuildOptions(), updateData);

    /// <summary>
    /// Выполняет пакетное обновление свойств сущностей на уровне БД (без предварительной загрузки экземпляров) по указанной спецификации.
    /// </summary>
    /// <param name="predicate">Условие фильтрации сущностей, к которым применяются обновления.</param>
    /// <param name="updateData">Массив кортежей, содержащий выражения для обновляемых свойств и их значений.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task ExecuteUpdateRangeAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        params (LambdaExpression propertyExpression, LambdaExpression valueExpression)[] updateData)
    {
        var options = new QueryOptions<TEntity>();
        if (predicate is not null)
        {
            options.AddFilter(predicate);
        }

        return ExecuteUpdateRangeAsync(options, updateData);
    }

    /// <summary>
    /// Создаёт кортеж выражений для использования в методах массового обновления.
    /// </summary>
    /// <typeparam name="TProp">Тип обновляемого свойства.</typeparam>
    /// <param name="propertyExpression">Выражение, указывающее на обновляемое свойство.</param>
    /// <param name="valueExpression">Выражение, вычисляющее новое значение свойства.</param>
    /// <returns>Кортеж из двух выражений: свойства и значения.</returns>
    /// <remarks>
    /// Пример использования:
    /// <code>
    /// await repository.ExecuteUpdateRangeAsync(
    ///     x => x.Id == targetId,
    ///     repository.GetUpdateRangeAsyncLambdaFunc(x => x.Name, x => "newName"),
    ///     repository.GetUpdateRangeAsyncLambdaFunc(x => x.Version, x => "v2"));
    /// </code>
    /// </remarks>
    public static (LambdaExpression, LambdaExpression) GetUpdateRangeAsyncLambdaFunc<TProp>(
        Expression<Func<TEntity, TProp>> propertyExpression,
        Expression<Func<TEntity, TProp>> valueExpression)
    {
        return (propertyExpression, valueExpression);
    }
}
