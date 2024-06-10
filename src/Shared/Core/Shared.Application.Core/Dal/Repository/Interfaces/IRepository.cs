// ----------------------------------------------------------------------------------------------
// <copyright file="IRepository.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dal.Repository.Models;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.Dal.Repository.Interfaces;

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
    /// <param name="options">Настройки запроса. Если null, то запрос будет выполнен без дополнительных настроек.</param>
    /// <returns>Экземпляр сущности, если найден, иначе null.</returns>
    Task<TEntity?> GetAsync(
        object id,
        QueryOptions<TEntity>? options = null);

    /// <summary>
    /// Асинхронно возвращает коллекцию экземпляров сущности по переданной настройке.
    /// </summary>
    /// <param name="options">Настройки запроса. Если null, то запрос будет выполнен без дополнительных настроек.</param>
    /// <param name="skip">Количество сущностей, которые необходимо пропустить.</param>
    /// <param name="take">Количество сущностей, которые необходимо извлечь.</param>
    /// <returns>Коллекция экземпляров сущности, полученная по переданной настройке.</returns>
    Task<List<TEntity>> GetRangeAsync(
        QueryOptions<TEntity> options, 
        int? skip = null, 
        int? take = null);

    /// <summary>
    /// Асинхронно возвращает коллекцию сущностей, которые были преобразованы в тип <typeparamref name="TOut"/>.
    /// </summary>
    /// <param name="options">Настройки запроса. Если null, то запрос будет выполнен без дополнительных настроек.</param>
    /// <param name="skip">Количество сущностей, которые необходимо пропустить.</param>
    /// <param name="take">Количество сущностей, которые необходимо извлечь.</param>
    /// <typeparam name="TOut">Тип, к которому будут преобразованы сущности.</typeparam>
    /// <returns>Коллекция сущностей, которые были преобразованы в тип <typeparamref name="TOut"/>.</returns>
    Task<List<TOut>> GetRangeAsync<TOut>(
        QueryOptions<TEntity> options,
        int? skip = null,
        int? take = null);

    /// <summary>
    /// Асинхронно возвращает первый попавшийся экземпляр сущности из выборки по переданной настройке.
    /// </summary>
    /// <param name="options">Настройки запроса. Если null, то запрос будет выполнен без дополнительных настроек.</param>
    /// <returns>Первый попавшийся экземпляр сущности из выборки по переданной спецификации, если выборка не пуста, иначе null.</returns>
    Task<TEntity?> FirstOrDefaultAsync(QueryOptions<TEntity> options);

    /// <summary>
    /// Асинхронно возвращает единственный экземпляр сущности из выборки по переданной настройке.
    /// </summary>
    /// <param name="options">Настройки запроса. Если null, то запрос будет выполнен без дополнительных настроек.</param>
    /// <exception cref="T:System.InvalidOperationException">Выборка по спецификации содержит более одного элемента.</exception>
    /// <returns>Единственный экземпляр сущности из выборки по переданной спецификации, если выборка имеет 1 элемент, иначе null.</returns>
    Task<TEntity?> SingleOrDefaultAsync(QueryOptions<TEntity> options);

    /// <summary>
    /// Асинхронно возвращает последний экземпляр сущности из выборки по переданной настройке.
    /// </summary>
    /// <param name="options">Настройки запроса. Если null, то запрос будет выполнен без дополнительных настроек.</param>
    /// <returns>Последний экземпляр сущности из выборки по переданной спецификации, если выборка не пуста, иначе null.</returns>
    Task<TEntity?> LastOrDefaultAsync(QueryOptions<TEntity> options);

    #endregion

    #region Utility methods

    /// <summary>
    /// Асинхронно возвращает количество элементов в выборке по настройке.
    /// </summary>
    /// <param name="options">Настройки запроса. Если null, то запрос будет выполнен без дополнительных настроек.</param>
    /// <returns>Количество элементов в выборке по спецификации.</returns>
    Task<int> CountAsync(QueryOptions<TEntity> options);

    #endregion

    #region Add methods

    /// <summary>
    /// Асинхронно добавляет экземпляр сущности в БД.
    /// </summary>
    /// <remarks>Имеет эффект только после вызова <see cref="SaveChangesAsync"/>.</remarks>
    /// <param name="entity">Экземпляр сущности.</param>
    /// <returns>Экземпляр созданной сущности.</returns>
    Task<TEntity> AddAsync(TEntity entity);

    /// <summary>
    /// Асинхронно добавляет коллекцию экземпляров сущности в БД.
    /// </summary>
    /// <remarks>Имеет эффект только после вызова <see cref="SaveChangesAsync"/>.</remarks>
    /// <param name="entities">Коллекция экземпляров сущности.</param>
    /// <returns></returns>
    Task AddRangeAsync(IEnumerable<TEntity> entities);

    #endregion

    #region Remove methods

    // Remove
    /// <summary>
    /// Асинхронно удаляет экземпляр сущности из БД.
    /// </summary>
    /// <remarks>Имеет эффект только после вызова <see cref="SaveChangesAsync"/>.</remarks>
    /// <param name="entity">Экземпляр сущности.</param>
    /// <returns></returns>
    Task RemoveAsync(TEntity entity);

    /// <summary>
    /// Асинхронно удаляет коллекцию экземпляров сущности из БД.
    /// </summary>
    /// <remarks>Имеет эффект только после вызова <see cref="SaveChangesAsync"/>.</remarks>
    /// <param name="entities">Коллекция экземпляров сущности.</param>
    /// <returns></returns>
    Task RemoveRangeAsync(IEnumerable<TEntity> entities);

    /// <summary>
    /// Асинхронно удаляет выборку экземпляров сущности из БД, по переданной настройке.
    /// </summary>
    /// <remarks>Имеет эффект только после вызова <see cref="SaveChangesAsync"/>.</remarks>
    /// <param name="options">Настройки запроса. Если null, то запрос будет выполнен без дополнительных настроек.</param>
    /// <returns></returns>
    Task RemoveRangeAsync(QueryOptions<TEntity> options);

    #endregion

    /// <summary>
    /// Выполняет операцию с сущностями.
    /// </summary>
    /// <typeparam name="TEntity">Тип сущностей, для которых будет выполнена операция.</typeparam>
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
    /// /// <typeparam name="TEntity"> Тип сущности, для которой будет выполнена операция. </typeparam>
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
    IQueryable<TEntity> Set();

    /// <summary>
    /// Применяет внесенные до вызова изменения.
    /// </summary>
    void SaveChanges();

    /// <summary>
    /// Асинхронно применяет внесенные до вызова изменения.
    /// </summary>
    Task SaveChangesAsync();
}
