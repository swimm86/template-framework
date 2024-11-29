// ----------------------------------------------------------------------------------------------
// <copyright file="IRepository.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
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
    /// Асинхронно возвращает экземпляр сущности по ее индентификатору.
    /// </summary>
    /// <param name="id">Идентификатор сущности.</param>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <returns>Экземпляр сущности, если найден, иначе null.</returns>
    Task<TEntity?> GetAsync(
        object id,
        QueryOptions<TEntity>? options = null);

    /// <summary>
    /// Асинхронно возвращает экземпляр сущности по ее индентификатору.
    /// </summary>
    /// <param name="id">Идентификатор сущности.</param>
    /// <param name="specification">Спецификация.</param>
    /// <returns>Экземпляр сущности, если найден, иначе null.</returns>
    Task<TEntity?> GetAsync(
        object id,
        ISpecification<TEntity> specification) =>
        GetAsync(id, specification.BuildOptions());

    /// <summary>
    /// Асинхронно возвращает коллекцию экземпляров сущности по переданной настройке.
    /// </summary>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <param name="skip">Количество сущностей, которые необходимо пропустить.</param>
    /// <param name="take">Количество сущностей, которые необходимо извлечь.</param>
    /// <returns>Коллекция экземпляров сущности, полученная по переданной настройке.</returns>
    Task<List<TEntity>> GetRangeAsync(
        QueryOptions<TEntity>? options = null,
        int? skip = null,
        int? take = null);

    /// <summary>
    /// Асинхронно возвращает коллекцию экземпляров сущности по переданной настройке.
    /// </summary>
    /// <param name="specification">Спецификация.</param>
    /// <param name="skip">Количество сущностей, которые необходимо пропустить.</param>
    /// <param name="take">Количество сущностей, которые необходимо извлечь.</param>
    /// <returns>Коллекция экземпляров сущности, полученная по переданной настройке.</returns>
    Task<List<TEntity>> GetRangeAsync(
        ISpecification<TEntity> specification,
        int? skip = null,
        int? take = null) =>
        GetRangeAsync(specification.BuildOptions(), skip, take);

    /// <summary>
    /// Асинхронно возвращает коллекцию сущностей, которые были преобразованы в тип <typeparamref name="TOut"/>.
    /// </summary>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <param name="skip">Количество сущностей, которые необходимо пропустить.</param>
    /// <param name="take">Количество сущностей, которые необходимо извлечь.</param>
    /// <typeparam name="TOut">Тип, к которому будут преобразованы сущности.</typeparam>
    /// <returns>Коллекция сущностей, которые были преобразованы в тип <typeparamref name="TOut"/>.</returns>
    Task<List<TOut>> GetRangeAsync<TOut>(
        QueryOptions<TEntity>? options = null,
        int? skip = null,
        int? take = null);

    /// <summary>
    /// Асинхронно возвращает коллекцию сущностей, которые были преобразованы в тип <typeparamref name="TOut"/>.
    /// </summary>
    /// <param name="specification">Спецификация.</param>
    /// <param name="skip">Количество сущностей, которые необходимо пропустить.</param>
    /// <param name="take">Количество сущностей, которые необходимо извлечь.</param>
    /// <typeparam name="TOut">Тип, к которому будут преобразованы сущности.</typeparam>
    /// <returns>Коллекция сущностей, которые были преобразованы в тип <typeparamref name="TOut"/>.</returns>
    Task<List<TOut>> GetRangeAsync<TOut>(
        ISpecification<TEntity> specification,
        int? skip = null,
        int? take = null) =>
        GetRangeAsync<TOut>(specification.BuildOptions(), skip, take);

    /// <summary>
    /// Асинхронно возвращает первый попавшийся экземпляр сущности из выборки по переданной настройке.
    /// </summary>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <returns>Первый попавшийся экземпляр сущности из выборки по переданной спецификации, если выборка не пуста, иначе null.</returns>
    Task<TEntity?> FirstOrDefaultAsync(QueryOptions<TEntity>? options = null);

    /// <summary>
    /// Асинхронно возвращает первый попавшийся экземпляр сущности из выборки по переданной настройке.
    /// </summary>
    /// <param name="specification">Спецификация.</param>
    /// <returns>Первый попавшийся экземпляр сущности из выборки по переданной спецификации, если выборка не пуста, иначе null.</returns>
    Task<TEntity?> FirstOrDefaultAsync(ISpecification<TEntity> specification) =>
        FirstOrDefaultAsync(specification.BuildOptions());

    /// <summary>
    /// Асинхронно возвращает первый попавшийся экземпляр сущности из выборки по переданной настройке с преоборазованием в тип <typeparamref name="TOut"/>.
    /// </summary>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <typeparam name="TOut">Тип, к которому будут преобразованы сущности.</typeparam>
    /// <returns>Первый попавшийся экземпляр сущности из выборки по переданной спецификации, если выборка не пуста, иначе null.</returns>
    Task<TOut?> FirstOrDefaultAsync<TOut>(QueryOptions<TEntity>? options = null);

    /// <summary>
    /// Асинхронно возвращает первый попавшийся экземпляр сущности из выборки по переданной настройке с преоборазованием в тип <typeparamref name="TOut"/>.
    /// </summary>
    /// <param name="specification">Спецификация.</param>
    /// <typeparam name="TOut">Тип, к которому будут преобразованы сущности.</typeparam>
    /// <returns>Первый попавшийся экземпляр сущности из выборки по переданной спецификации, если выборка не пуста, иначе null.</returns>
    Task<TOut?> FirstOrDefaultAsync<TOut>(ISpecification<TEntity> specification) =>
        FirstOrDefaultAsync<TOut>(specification.BuildOptions());

    /// <summary>
    /// Асинхронно возвращает единственный экземпляр сущности из выборки по переданной настройке.
    /// </summary>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <exception cref="T:System.InvalidOperationException">Выборка по спецификации содержит более одного элемента.</exception>
    /// <returns>Единственный экземпляр сущности из выборки по переданной спецификации, если выборка имеет 1 элемент, иначе null.</returns>
    Task<TEntity?> SingleOrDefaultAsync(QueryOptions<TEntity>? options = null);

    /// <summary>
    /// Асинхронно возвращает единственный экземпляр сущности из выборки по переданной настройке.
    /// </summary>
    /// <exception cref="T:System.InvalidOperationException">Выборка по спецификации содержит более одного элемента.</exception>
    /// <param name="specification">Спецификация.</param>
    /// <returns>Единственный экземпляр сущности из выборки по переданной спецификации, если выборка имеет 1 элемент, иначе null.</returns>
    Task<TEntity?> SingleOrDefaultAsync(ISpecification<TEntity> specification) =>
        SingleOrDefaultAsync(specification.BuildOptions());

    /// <summary>
    /// Асинхронно возвращает единственный экземпляр сущности из выборки по переданной настройке с преоборазованием в тип <typeparamref name="TOut"/>.
    /// </summary>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <typeparam name="TOut">Тип, к которому будут преобразованы сущности.</typeparam>
    /// <exception cref="T:System.InvalidOperationException">Выборка по спецификации содержит более одного элемента.</exception>
    /// <returns>Единственный экземпляр сущности из выборки по переданной спецификации, если выборка имеет 1 элемент, иначе null.</returns>
    Task<TOut?> SingleOrDefaultAsync<TOut>(QueryOptions<TEntity>? options = null);

    /// <summary>
    /// Асинхронно возвращает единственный экземпляр сущности из выборки по переданной настройке с преоборазованием в тип <typeparamref name="TOut"/>.
    /// </summary>
    /// <exception cref="T:System.InvalidOperationException">Выборка по спецификации содержит более одного элемента.</exception>
    /// <param name="specification">Спецификация.</param>
    /// <typeparam name="TOut">Тип, к которому будут преобразованы сущности.</typeparam>
    /// <returns>Единственный экземпляр сущности из выборки по переданной спецификации, если выборка имеет 1 элемент, иначе null.</returns>
    Task<TOut?> SingleOrDefaultAsync<TOut>(ISpecification<TEntity> specification) =>
        SingleOrDefaultAsync<TOut>(specification.BuildOptions());

    /// <summary>
    /// Асинхронно возвращает последний экземпляр сущности из выборки по переданной настройке.
    /// </summary>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <returns>Последний экземпляр сущности из выборки по переданной спецификации, если выборка не пуста, иначе null.</returns>
    Task<TEntity?> LastOrDefaultAsync(QueryOptions<TEntity>? options = null);

    /// <summary>
    /// Асинхронно возвращает последний экземпляр сущности из выборки по переданной настройке.
    /// </summary>
    /// <param name="specification">Спецификация.</param>
    /// <returns>Последний экземпляр сущности из выборки по переданной спецификации, если выборка не пуста, иначе null.</returns>
    Task<TEntity?> LastOrDefaultAsync(ISpecification<TEntity> specification) =>
        LastOrDefaultAsync(specification.BuildOptions());

    /// <summary>
    /// Асинхронно возвращает последний экземпляр сущности из выборки по переданной настройке с преоборазованием в тип <typeparamref name="TOut"/>.
    /// </summary>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <typeparam name="TOut">Тип, к которому будут преобразованы сущности.</typeparam>
    /// <returns>Последний экземпляр сущности из выборки по переданной спецификации, если выборка не пуста, иначе null.</returns>
    Task<TOut?> LastOrDefaultAsync<TOut>(QueryOptions<TEntity>? options = null);

    /// <summary>
    /// Асинхронно возвращает последний экземпляр сущности из выборки по переданной настройке с преоборазованием в тип <typeparamref name="TOut"/>.
    /// </summary>
    /// <param name="specification">Спецификация.</param>
    /// <typeparam name="TOut">Тип, к которому будут преобразованы сущности.</typeparam>
    /// <returns>Последний экземпляр сущности из выборки по переданной спецификации, если выборка не пуста, иначе null.</returns>
    Task<TOut?> LastOrDefaultAsync<TOut>(ISpecification<TEntity> specification) =>
        LastOrDefaultAsync<TOut>(specification.BuildOptions());

    #endregion

    #region Aggregation methods

    /// <summary>
    /// Асинхронно возвращает количество элементов в выборке по настройке.
    /// </summary>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <returns>Количество элементов в выборке по спецификации.</returns>
    Task<int> CountAsync(QueryOptions<TEntity>? options = null);

    /// <summary>
    /// Асинхронно возвращает количество элементов в выборке по настройке (Specification).
    /// </summary>
    /// <param name="specification">Спецификация.</param>
    /// <returns>Количество элементов в выборке по спецификации.</returns>
    Task<int> CountAsync(ISpecification<TEntity> specification) =>
        CountAsync(specification.BuildOptions());

    /// <summary>
    /// Асинхронно возвращает признак наличия элемента в выборке по настройке.
    /// </summary>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <returns>Признак наличия элемента в выборке по настройке</returns>
    Task<bool> AnyAsync(QueryOptions<TEntity>? options = null);

    /// <summary>
    /// Асинхронно возвращает признак наличия элемента в выборке по настройке (Specification).
    /// </summary>
    /// <param name="specification">Спецификация.</param>
    /// <returns>Признак наличия элемента в выборке по настройке</returns>
    Task<bool> AnyAsync(ISpecification<TEntity> specification) =>
        AnyAsync(specification.BuildOptions());

    /// <summary>
    /// Асинхронно высисляет сумму проекций элементов (в выборке по настройке) в числовые значения.
    /// </summary>
    /// <param name="selector">Проекция, применяемая к каждому элементу.</param>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <returns>Сумму проекций элементов (в выборке по настройке) в числовые значения</returns>
    Task<decimal> SumAsync(Expression<Func<TEntity, decimal>> selector, QueryOptions<TEntity>? options = null);

    /// <summary>
    /// Асинхронно высисляет сумму проекций элементов (в выборке по настройке) в числовые значения.
    /// </summary>
    /// <param name="selector">Проекция, применяемая к каждому элементу.</param>
    /// <param name="specification">Спецификация.</param>
    /// <returns>Сумму проекций элементов (в выборке по настройке) в числовые значения</returns>
    Task<decimal> SumAsync(Expression<Func<TEntity, decimal>> selector, ISpecification<TEntity> specification) =>
        SumAsync(selector, specification.BuildOptions());

    #endregion

    #region Add methods

    /// <summary>
    /// Асинхронно добавляет экземпляр сущности в БД.
    /// </summary>
    /// <remarks>Имеет эффект только после вызова <see cref="SaveChangesAsync"/>.</remarks>
    /// <param name="entity">Экземпляр сущности.</param>
    /// <param name="userId">Id пользователя, добавившего запись.</param>
    /// <returns>Экземпляр созданной сущности.</returns>
    Task<TEntity> AddAsync(TEntity entity, Guid? userId);

    /// <summary>
    /// Асинхронно добавляет коллекцию экземпляров сущности в БД.
    /// </summary>
    /// <remarks>Имеет эффект только после вызова <see cref="SaveChangesAsync"/>.</remarks>
    /// <param name="entities">Коллекция экземпляров сущности.</param>
    /// <param name="userId">Id пользователя, добавившего запись.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task AddRangeAsync(IEnumerable<TEntity> entities, Guid? userId);

    #endregion

    #region Remove methods

    /// <summary>
    /// Асинхронно удаляет экземпляр сущности из БД.
    /// </summary>
    /// <remarks>Имеет эффект только после вызова <see cref="SaveChangesAsync"/>.</remarks>
    /// <param name="entity">Экземпляр сущности.</param>
    /// <param name="userId">Id пользователя, удалившего запись.</param>
    /// <param name="hard">Признак того, что сущность должна быть удалена физически.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task RemoveAsync(TEntity entity, Guid? userId, bool hard = false);

    /// <summary>
    /// Асинхронно удаляет экземпляр сущности из БД.
    /// </summary>
    /// <remarks>Имеет эффект только после вызова <see cref="SaveChangesAsync"/>.</remarks>
    /// <param name="entity">Экземпляр сущности.</param>
    /// <param name="hard">Признак того, что сущность должна быть удалена физически.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task RemoveAsync(TEntity entity, bool hard = false);

    /// <summary>
    /// Асинхронно удаляет коллекцию экземпляров сущности из БД.
    /// </summary>
    /// <remarks>Имеет эффект только после вызова <see cref="SaveChangesAsync"/>.</remarks>
    /// <param name="entities">Коллекция экземпляров сущности.</param>
    /// <param name="hard">Признак того, что сущность должна быть удалена физически.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task RemoveRangeAsync(IEnumerable<TEntity> entities, bool hard = false);

    /// <summary>
    /// Асинхронно физически удаляет коллекцию экземпляров сущности из БД.
    /// </summary>
    /// <remarks>Имеет эффект только после вызова <see cref="SaveChangesAsync"/>.</remarks>
    /// <param name="entities">Коллекция экземпляров сущности.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task RemovePermanentRangeAsync(IEnumerable<TEntity> entities);

    /// <summary>
    /// Асинхронно удаляет выборку экземпляров сущности из БД, по переданной настройке.
    /// </summary>
    /// <remarks>Имеет эффект только после вызова <see cref="SaveChangesAsync"/>.</remarks>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <param name="hard">Признак того, что сущность должна быть удалена физически.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task RemoveRangeAsync(QueryOptions<TEntity> options, bool hard = false);

    /// <summary>
    /// Асинхронно удаляет выборку экземпляров сущности из БД, по переданному условию.
    /// </summary>
    /// <remarks>Имеет эффект только после вызова <see cref="SaveChangesAsync"/>.</remarks>
    /// <param name="conditions">Условия для удаления.</param>
    /// <param name="hard">Признак того, что сущность должна быть удалена физически.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task RemoveRangeAsync(Expression<Func<TEntity, bool>> conditions, bool hard = false);

    /// <summary>
    /// Асинхронно удаляет выборку экземпляров сущности из БД, по переданной настройке (Specification).
    /// </summary>
    /// <remarks>Имеет эффект только после вызова <see cref="SaveChangesAsync"/>.</remarks>
    /// <param name="specification">Спецификация. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <param name="hard">Признак того, что сущность должна быть удалена физически.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task RemoveRangeAsync(ISpecification<TEntity> specification, bool hard = false) =>
        RemoveRangeAsync(specification.BuildOptions());

    #endregion

    /// <summary>
    /// Выполняет операцию с сущностями.
    /// </summary>
    /// <typeparam name="TResult">Тип результата выполнения операции.</typeparam>
    /// <param name="process">Реализация операции.</param>
    /// <param name="useTransaction">Признак того, что операция будет выполнена в рамках транзакции.</param>
    /// <returns>Результат выполнения операции <typeparamref name="TResult"/>.</returns>
    TResult Execute<TResult>(
        Func<TResult> process,
        bool useTransaction = false);

    /// <summary>
    /// Выполняет операцию асинхронно
    /// </summary>
    /// <typeparam name="TResult">Тип результата выполнения операции.</typeparam>
    /// <param name="process">Асинхрорнная реализация операции.</param>
    /// <param name="token">Токен отмены операции.</param>
    /// <param name="useTransaction">Признак того, что операция будет выполнена в рамках транзакции.</param>
    /// <returns>Результат выполнения операции <see cref="TResult"/>.</returns>
    Task<TResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> process,
        CancellationToken token,
        bool useTransaction = false);

    /// <summary>
    /// Возвращает <see cref="IQueryable{TEntity}"/>.
    /// </summary>
    /// <returns><see cref="IQueryable{TEntity}"/>.</returns>
    IQueryable<TEntity> Set();

    /// <summary>
    /// Применяет внесенные до вызова изменения.
    /// </summary>
    void SaveChanges();

    /// <summary>
    /// Асинхронно применяет внесенные до вызова изменения.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    Task SaveChangesAsync();
}
