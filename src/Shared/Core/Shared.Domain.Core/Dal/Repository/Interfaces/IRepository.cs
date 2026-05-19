// ----------------------------------------------------------------------------------------------
// <copyright file="IRepository.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Dal.Specification.Interfaces;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Dal.Repository.Interfaces;

/// <summary>
/// Интерфейс, предоставляющий репозиторий данных.
/// </summary>
/// <typeparam name="TEntity">Тип сущности.</typeparam>
public interface IRepository<TEntity>
    where TEntity : class, IEntity
{
    #region Read methods

    /// <summary>
    /// Асинхронно возвращает экземпляр сущности по ее идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор сущности.</param>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Экземпляр сущности, если найден, иначе null.</returns>
    Task<TEntity?> GetAsync(
        object? id,
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default);

    /// <param name="specification">Спецификация.</param>
    /// <returns>Экземпляр сущности, если найден, иначе null.</returns>
    /// <inheritdoc cref="GetAsync(object?, QueryOptions{TEntity}?, CancellationToken)"/>
    /// <param name="id"/><param name="cancellationToken"/>
    Task<TEntity?> GetAsync(
        object? id,
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default) =>
        GetAsync(id, specification.BuildOptions(), cancellationToken);

    /// <summary>
    /// Асинхронно возвращает экземпляр сущности, преобразованный в тип <typeparamref name="TOut"/>, по ее идентификатору.
    /// </summary>
    /// <typeparam name="TOut">Тип, к которому будут преобразована сущность.</typeparam>
    /// <param name="id">Идентификатор сущности.</param>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <param name="selector">Преобразование (если null, то используется преобразование с помощью маппера).</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Экземпляр сущности, преобразованный в тип <typeparamref name="TOut"/>, если найден, иначе null.</returns>
    Task<TOut?> GetAsync<TOut>(
        object? id,
        QueryOptions<TEntity>? options = null,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default);

    /// <param name="specification">Спецификация.</param>
    /// <inheritdoc cref="GetAsync{TOut}(object?, QueryOptions{TEntity}?, Expression{Func{TEntity, TOut}}?, CancellationToken)"/>
    /// <param name="id"/><param name="selector"/><param name="cancellationToken"/>
    Task<TOut?> GetAsync<TOut>(
        object? id,
        ISpecification<TEntity> specification,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default) =>
        GetAsync(id, specification.BuildOptions(), selector, cancellationToken);

    /// <summary>
    /// Асинхронно возвращает коллекцию сущностей по заданным параметрам запроса.
    /// </summary>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <param name="skip">Количество сущностей, которые необходимо пропустить.</param>
    /// <param name="take">Количество сущностей, которые необходимо извлечь.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Коллекция сущностей, отфильтрованных и упорядоченных согласно заданным параметрам.</returns>
    Task<List<TEntity>> GetRangeAsync(
        QueryOptions<TEntity>? options = null,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default);

    /// <param name="specification">Спецификация.</param>
    /// <returns>Коллекция сущностей, отфильтрованных и упорядоченных согласно заданной спецификации.</returns>
    /// <inheritdoc cref="GetRangeAsync(QueryOptions{TEntity}?, int?, int?, CancellationToken)"/>
    /// <param name="skip"/><param name="take"/><param name="cancellationToken"/>
    Task<List<TEntity>> GetRangeAsync(
        ISpecification<TEntity> specification,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default) =>
        GetRangeAsync(specification.BuildOptions(), skip, take, cancellationToken);

    /// <summary>
    /// Асинхронно возвращает коллекцию сущностей, преобразованных в тип <typeparamref name="TOut"/>, по заданным параметрам запроса.
    /// </summary>
    /// <typeparam name="TOut">Тип, к которому будут преобразованы сущности.</typeparam>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <param name="skip">Количество сущностей, которые необходимо пропустить.</param>
    /// <param name="take">Количество сущностей, которые необходимо извлечь.</param>
    /// <param name="selector">Выражение проекции (если null, используется маппер).</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Коллекция преобразованных сущностей, отфильтрованных и упорядоченных согласно заданным параметрам.</returns>
    Task<List<TOut>> GetRangeAsync<TOut>(
        QueryOptions<TEntity>? options = null,
        int? skip = null,
        int? take = null,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default);

    /// <param name="specification">Спецификация.</param>
    /// <inheritdoc cref="GetRangeAsync{TOut}(QueryOptions{TEntity}?, int?, int?, Expression{Func{TEntity, TOut}}?, CancellationToken)"/>
    /// <param name="skip"/><param name="take"/><param name="selector"/><param name="cancellationToken"/>
    Task<List<TOut>> GetRangeAsync<TOut>(
        ISpecification<TEntity> specification,
        int? skip = null,
        int? take = null,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default) =>
        GetRangeAsync(specification.BuildOptions(), skip, take, selector, cancellationToken);

    /// <summary>
    /// Асинхронно возвращает первый элемент выборки или null, если выборка пуста.
    /// </summary>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Первый элемент выборки или null, если выборка пуста.</returns>
    Task<TEntity?> FirstOrDefaultAsync(
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default);

    /// <param name="specification">Спецификация.</param>
    /// <returns>Первый элемент выборки или null, если выборка пуста.</returns>
    /// <inheritdoc cref="FirstOrDefaultAsync(QueryOptions{TEntity}?, CancellationToken)"/>
    /// <param name="cancellationToken"/>
    Task<TEntity?> FirstOrDefaultAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default) =>
        FirstOrDefaultAsync(specification.BuildOptions(), cancellationToken);

    /// <summary>
    /// Асинхронно возвращает первый элемент выборки, преобразованный в тип <typeparamref name="TOut"/>, или null, если выборка пуста.
    /// </summary>
    /// <typeparam name="TOut">Тип, к которому будет преобразована сущность.</typeparam>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <param name="selector">Выражение проекции (если null, используется маппер).</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Первый преобразованный элемент выборки или null, если выборка пуста.</returns>
    Task<TOut?> FirstOrDefaultAsync<TOut>(
        QueryOptions<TEntity>? options = null,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default);

    /// <param name="specification">Спецификация.</param>
    /// <inheritdoc cref="FirstOrDefaultAsync{TOut}(QueryOptions{TEntity}?, Expression{Func{TEntity, TOut}}?, CancellationToken)"/>
    /// <param name="selector"/><param name="cancellationToken"/>
    Task<TOut?> FirstOrDefaultAsync<TOut>(
        ISpecification<TEntity> specification,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default) =>
        FirstOrDefaultAsync(specification.BuildOptions(), selector, cancellationToken);

    /// <summary>
    /// Асинхронно возвращает единственный элемент выборки или null, если выборка пуста.
    /// </summary>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Единственный элемент выборки или null, если выборка пуста.</returns>
    /// <exception cref="InvalidOperationException">Выборка содержит более одного элемента.</exception>
    Task<TEntity?> SingleOrDefaultAsync(
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default);

    /// <param name="specification">Спецификация.</param>
    /// <returns>Единственный элемент выборки или null, если выборка пуста.</returns>
    /// <inheritdoc cref="SingleOrDefaultAsync(QueryOptions{TEntity}?, CancellationToken)"/>
    /// <param name="cancellationToken"/>
    Task<TEntity?> SingleOrDefaultAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default) =>
        SingleOrDefaultAsync(specification.BuildOptions(), cancellationToken);

    /// <summary>
    /// Асинхронно возвращает единственный элемент выборки, преобразованный в тип <typeparamref name="TOut"/>, или null, если выборка пуста.
    /// </summary>
    /// <typeparam name="TOut">Тип, к которому будет преобразована сущность.</typeparam>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <param name="selector">Выражение проекции (если null, используется маппер).</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Единственный преобразованный элемент выборки или null, если выборка пуста.</returns>
    /// <exception cref="InvalidOperationException">Выборка содержит более одного элемента.</exception>
    Task<TOut?> SingleOrDefaultAsync<TOut>(
        QueryOptions<TEntity>? options = null,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default);

    /// <param name="specification">Спецификация.</param>
    /// <inheritdoc cref="SingleOrDefaultAsync{TOut}(QueryOptions{TEntity}?, Expression{Func{TEntity, TOut}}?, CancellationToken)"/>
    /// <param name="selector"/><param name="cancellationToken"/>
    Task<TOut?> SingleOrDefaultAsync<TOut>(
        ISpecification<TEntity> specification,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default) =>
        SingleOrDefaultAsync(specification.BuildOptions(), selector, cancellationToken);

    /// <summary>
    /// Асинхронно возвращает последний элемент выборки или null, если выборка пуста.
    /// </summary>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Последний элемент выборки или null, если выборка пуста.</returns>
    Task<TEntity?> LastOrDefaultAsync(
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default);

    /// <param name="specification">Спецификация.</param>
    /// <returns>Последний элемент выборки или null, если выборка пуста.</returns>
    /// <inheritdoc cref="LastOrDefaultAsync(QueryOptions{TEntity}?, CancellationToken)"/>
    /// <param name="cancellationToken"/>
    Task<TEntity?> LastOrDefaultAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default) =>
        LastOrDefaultAsync(specification.BuildOptions(), cancellationToken);

    /// <summary>
    /// Асинхронно возвращает последний элемент выборки, преобразованный в тип <typeparamref name="TOut"/>, или null, если выборка пуста.
    /// </summary>
    /// <typeparam name="TOut">Тип, к которому будет преобразована сущность.</typeparam>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <param name="selector">Выражение проекции (если null, используется маппер).</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Последний преобразованный элемент выборки или null, если выборка пуста.</returns>
    Task<TOut?> LastOrDefaultAsync<TOut>(
        QueryOptions<TEntity>? options = null,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default);

    /// <param name="specification">Спецификация.</param>
    /// <inheritdoc cref="LastOrDefaultAsync{TOut}(QueryOptions{TEntity}?, Expression{Func{TEntity, TOut}}?, CancellationToken)"/>
    /// <param name="selector"/><param name="cancellationToken"/>
    Task<TOut?> LastOrDefaultAsync<TOut>(
        ISpecification<TEntity> specification,
        Expression<Func<TEntity, TOut>>? selector = null,
        CancellationToken cancellationToken = default) =>
        LastOrDefaultAsync(specification.BuildOptions(), selector, cancellationToken);

    #endregion

    #region Aggregation methods

    /// <summary>
    /// Асинхронно возвращает количество элементов в выборке.
    /// </summary>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Количество элементов в выборке.</returns>
    Task<int> CountAsync(
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default);

    /// <param name="specification">Спецификация.</param>
    /// <returns>Количество элементов в выборке.</returns>
    /// <inheritdoc cref="CountAsync(QueryOptions{TEntity}?, CancellationToken)"/>
    /// <param name="cancellationToken"/>
    Task<int> CountAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default) =>
        CountAsync(specification.BuildOptions(), cancellationToken);

    /// <summary>
    /// Асинхронно возвращает признак наличия хотя бы одного элемента в выборке.
    /// </summary>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Значение true, если выборка содержит хотя бы один элемент, иначе false.</returns>
    Task<bool> AnyAsync(
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default);

    /// <param name="specification">Спецификация.</param>
    /// <returns>Значение true, если выборка содержит хотя бы один элемент, иначе false.</returns>
    /// <inheritdoc cref="AnyAsync(QueryOptions{TEntity}?, CancellationToken)"/>
    /// <param name="cancellationToken"/>
    Task<bool> AnyAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default) =>
        AnyAsync(specification.BuildOptions(), cancellationToken);

    /// <summary>
    /// Асинхронно вычисляет сумму проекций элементов выборки в числовые значения.
    /// </summary>
    /// <param name="selector">Выражение проекции, применяемое к каждому элементу.</param>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Сумма проекций элементов выборки.</returns>
    Task<decimal> SumAsync(
        Expression<Func<TEntity, decimal>> selector,
        QueryOptions<TEntity>? options = null,
        CancellationToken cancellationToken = default);

    /// <param name="specification">Спецификация.</param>
    /// <returns>Сумма проекций элементов выборки.</returns>
    /// <inheritdoc cref="SumAsync(Expression{Func{TEntity, decimal}}, QueryOptions{TEntity}?, CancellationToken)"/>
    /// <param name="selector"/><param name="cancellationToken"/>
    Task<decimal> SumAsync(
        Expression<Func<TEntity, decimal>> selector,
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default) =>
        SumAsync(selector, specification.BuildOptions(), cancellationToken);

    #endregion

    #region Add methods

    /// <summary>
    /// Асинхронно добавляет экземпляр сущности в БД.
    /// </summary>
    /// <remarks>Имеет эффект только после вызова <see cref="SaveChangesAsync"/>.</remarks>
    /// <param name="entity">Экземпляр сущности.</param>
    /// <param name="userId">Id пользователя, добавившего запись.</param>
    /// <param name="userName">Имя пользователя, добавившего запись.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Экземпляр созданной сущности.</returns>
    Task<TEntity> AddAsync(
        TEntity entity,
        Guid? userId = null,
        string? userName = null,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="AddAsync(TEntity, Guid?, string?, CancellationToken)"/>
    /// <param name="entities">Коллекция экземпляров сущности.</param>
    /// <param name="userId"/><param name="userName"/><param name="cancellationToken"/>
    Task AddRangeAsync(
        IEnumerable<TEntity> entities,
        Guid? userId = null,
        string? userName = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Update methods

    /// <summary>
    /// Массово обновляет сущности, удовлетворяющие заданному условию.
    /// </summary>
    /// <param name="condition">Выражение-условие для отбора обновляемых сущностей.</param>
    /// <param name="updateData">Массив кортежей: выражение свойства и выражение нового значения.</param>
    /// <returns>Задача выполнения операции обновления.</returns>
    Task UpdateRangeAsync(
        Expression<Func<TEntity, bool>>? condition = null,
        params (LambdaExpression propertyExpression, LambdaExpression valueExpression)[] updateData);

    /// <summary>
    /// Массово обновляет сущности по заданным параметрам запроса.
    /// </summary>
    /// <param name="options">Параметры запроса для отбора обновляемых сущностей.</param>
    /// <param name="updateData">Массив кортежей: выражение свойства и выражение нового значения.</param>
    /// <returns>Задача выполнения операции обновления.</returns>
    Task UpdateRangeAsync(
        QueryOptions<TEntity> options,
        params (LambdaExpression propertyExpression, LambdaExpression valueExpression)[] updateData);

    /// <summary>
    /// Массово обновляет сущности, отобранные по заданной спецификации.
    /// </summary>
    /// <param name="specification">Спецификация для отбора обновляемых сущностей.</param>
    /// <param name="updateData">Массив кортежей: выражение свойства и выражение нового значения.</param>
    /// <returns>Задача выполнения операции обновления.</returns>
    Task UpdateRangeAsync(
        ISpecification<TEntity> specification,
        params (LambdaExpression propertyExpression, LambdaExpression valueExpression)[] updateData);

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
    /// await repository.UpdateRangeAsync(
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

    #endregion

    #region Remove methods

    /// <summary>
    /// Асинхронно удаляет экземпляр сущности из БД.
    /// </summary>
    /// <remarks>Имеет эффект только после вызова <see cref="SaveChangesAsync"/>.</remarks>
    /// <param name="entity">Экземпляр сущности.</param>
    /// <param name="userId">Id пользователя, удалившего запись.</param>
    /// <param name="hard">Признак того, что сущность должна быть удалена физически.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task RemoveAsync(
        TEntity entity,
        Guid? userId,
        bool hard = false,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="RemoveAsync(TEntity, Guid?, bool, CancellationToken)"/>
    /// <param name="entity"/><param name="hard"/><param name="cancellationToken"/>
    Task RemoveAsync(
        TEntity entity,
        bool hard = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Асинхронно удаляет коллекцию экземпляров сущности из БД.
    /// </summary>
    /// <remarks>Имеет эффект только после вызова <see cref="SaveChangesAsync"/>.</remarks>
    /// <param name="entities">Коллекция экземпляров сущности.</param>
    /// <param name="hard">Признак того, что сущность должна быть удалена физически.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task RemoveRangeAsync(
        IEnumerable<TEntity> entities,
        bool hard = false,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="RemoveRangeAsync(IEnumerable{TEntity}, bool, CancellationToken)"/>
    /// <param name="entities"/><param name="cancellationToken"/>
    /// <remarks>Сущности удаляются физически, мягкое удаление не применяется.</remarks>
    Task RemovePermanentRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Асинхронно удаляет сущности, отобранные по заданным параметрам запроса.
    /// </summary>
    /// <remarks>Имеет эффект только после вызова <see cref="SaveChangesAsync"/>.</remarks>
    /// <param name="options">Параметры запроса для отбора удаляемых сущностей.</param>
    /// <param name="hard">Удалить сущности физически (true) или применить мягкое удаление (false).</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Задача выполнения операции удаления.</returns>
    Task RemoveRangeAsync(
        QueryOptions<TEntity> options,
        bool hard = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Асинхронно удаляет сущности, удовлетворяющие заданному условию.
    /// </summary>
    /// <remarks>Имеет эффект только после вызова <see cref="SaveChangesAsync"/>.</remarks>
    /// <param name="conditions">Выражение-условие для отбора удаляемых сущностей.</param>
    /// <param name="hard">Удалить сущности физически (true) или применить мягкое удаление (false).</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Задача выполнения операции удаления.</returns>
    Task RemoveRangeAsync(
        Expression<Func<TEntity, bool>> conditions,
        bool hard = false,
        CancellationToken cancellationToken = default);

    /// <param name="specification">Спецификация для отбора удаляемых сущностей.</param>
    /// <inheritdoc cref="RemoveRangeAsync(QueryOptions{TEntity}, bool, CancellationToken)"/>
    /// <param name="hard"/><param name="cancellationToken"/>
    Task RemoveRangeAsync(
        ISpecification<TEntity> specification,
        bool hard = false,
        CancellationToken cancellationToken = default) =>
        RemoveRangeAsync(specification.BuildOptions(), hard, cancellationToken);

    #endregion

    /// <summary>
    /// Выполняет синхронную операцию в контексте репозитория.
    /// </summary>
    /// <param name="process">Делегат операции для выполнения.</param>
    /// <param name="useTransaction">Выполнять операцию в рамках транзакции.</param>
    void Execute(
        Action process,
        bool useTransaction = false) =>
        Execute<object?>(() =>
        {
            process();
            return null;
        });

    /// <summary>
    /// Выполняет синхронную операцию в контексте репозитория с возвращением результата.
    /// </summary>
    /// <typeparam name="TResult">Тип возвращаемого значения.</typeparam>
    /// <param name="process">Делегат операции для выполнения.</param>
    /// <param name="useTransaction">Выполнять операцию в рамках транзакции.</param>
    /// <returns>Результат выполнения операции.</returns>
    TResult Execute<TResult>(
        Func<TResult> process,
        bool useTransaction = false);

    /// <summary>
    /// Выполняет асинхронную операцию в контексте репозитория.
    /// </summary>
    /// <param name="process">Асинхронный делегат операции для выполнения.</param>
    /// <param name="useTransaction">Выполнять операцию в рамках транзакции.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Задача выполнения операции.</returns>
    Task ExecuteAsync(
        Func<Task> process,
        bool useTransaction = false,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync<object?>(
            async () =>
            {
                await process();
                return null;
            },
            useTransaction,
            cancellationToken);

    /// <summary>
    /// Выполняет асинхронную операцию в контексте репозитория с возвращением результата.
    /// </summary>
    /// <typeparam name="TResult">Тип возвращаемого значения.</typeparam>
    /// <param name="process">Асинхронный делегат операции для выполнения.</param>
    /// <param name="useTransaction">Выполнять операцию в рамках транзакции.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Результат выполнения операции.</returns>
    Task<TResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> process,
        bool useTransaction = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает <see cref="IQueryable{TEntity}"/> для построения запросов.
    /// </summary>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <returns>Запрос <see cref="IQueryable{TEntity}"/> с применёнными настройками.</returns>
    IQueryable<TEntity> Set(QueryOptions<TEntity>? options = null);

    /// <summary>
    /// Синхронно сохраняет все изменения, внесённые в контекст репозитория.
    /// </summary>
    void SaveChanges();

    /// <summary>
    /// Асинхронно сохраняет все изменения, внесённые в контекст репозитория.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> для отмены операции.</param>
    /// <returns>Задача выполнения операции сохранения.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
