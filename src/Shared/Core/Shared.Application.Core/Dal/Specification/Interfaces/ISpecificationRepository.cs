// ----------------------------------------------------------------------------------------------
// <copyright file="ISpecificationRepository.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.Dal.Specification.Interfaces;

/// <summary>
/// Интерфейс, предоставляющий репозиторий данных с использованием спецификаций.
/// </summary>
/// <typeparam name="TEntity">Тип сущности.</typeparam>
public interface ISpecificationRepository<TEntity>
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
        ISpecification<TEntity>? options = null);

    /// <summary>
    /// Асинхронно возвращает коллекцию экземпляров сущности по переданной настройке.
    /// </summary>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <param name="skip">Количество сущностей, которые необходимо пропустить.</param>
    /// <param name="take">Количество сущностей, которые необходимо извлечь.</param>
    /// <returns>Коллекция экземпляров сущности, полученная по переданной настройке.</returns>
    Task<List<TEntity>> GetRangeAsync(
        ISpecification<TEntity> options,
        int? skip = null,
        int? take = null);

    /// <summary>
    /// Асинхронно возвращает коллекцию сущностей, которые были преобразованы в тип <typeparamref name="TOut"/>.
    /// </summary>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <param name="skip">Количество сущностей, которые необходимо пропустить.</param>
    /// <param name="take">Количество сущностей, которые необходимо извлечь.</param>
    /// <typeparam name="TOut">Тип, к которому будут преобразованы сущности.</typeparam>
    /// <returns>Коллекция сущностей, которые были преобразованы в тип <typeparamref name="TOut"/>.</returns>
    Task<List<TOut>> GetRangeAsync<TOut>(
        ISpecification<TEntity> options,
        int? skip = null,
        int? take = null);

    /// <summary>
    /// Асинхронно возвращает первый попавшийся экземпляр сущности из выборки по переданной настройке.
    /// </summary>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <returns>Первый попавшийся экземпляр сущности из выборки по переданной спецификации, если выборка не пуста, иначе null.</returns>
    Task<TEntity?> FirstOrDefaultAsync(ISpecification<TEntity> options);

    /// <summary>
    /// Асинхронно возвращает единственный экземпляр сущности из выборки по переданной настройке.
    /// </summary>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <exception cref="T:System.InvalidOperationException">Выборка по спецификации содержит более одного элемента.</exception>
    /// <returns>Единственный экземпляр сущности из выборки по переданной спецификации, если выборка имеет 1 элемент, иначе null.</returns>
    Task<TEntity?> SingleOrDefaultAsync(ISpecification<TEntity> options);

    /// <summary>
    /// Асинхронно возвращает последний экземпляр сущности из выборки по переданной настройке.
    /// </summary>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <returns>Последний экземпляр сущности из выборки по переданной спецификации, если выборка не пуста, иначе null.</returns>
    Task<TEntity?> LastOrDefaultAsync(ISpecification<TEntity> options);

    #endregion

    #region Utility methods

    /// <summary>
    /// Асинхронно возвращает количество элементов в выборке по настройке.
    /// </summary>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <returns>Количество элементов в выборке по спецификации.</returns>
    Task<int> CountAsync(ISpecification<TEntity> options);

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
    /// <returns><see cref="Task"/>.</returns>
    Task AddRangeAsync(IEnumerable<TEntity> entities);

    #endregion

    #region Remove methods

    /// <summary>
    /// Асинхронно удаляет экземпляр сущности из БД.
    /// </summary>
    /// <remarks>Имеет эффект только после вызова <see cref="SaveChangesAsync"/>.</remarks>
    /// <param name="entity">Экземпляр сущности.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task RemoveAsync(TEntity entity);

    /// <summary>
    /// Асинхронно удаляет коллекцию экземпляров сущности из БД.
    /// </summary>
    /// <remarks>Имеет эффект только после вызова <see cref="SaveChangesAsync"/>.</remarks>
    /// <param name="entities">Коллекция экземпляров сущности.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task RemoveRangeAsync(IEnumerable<TEntity> entities);

    /// <summary>
    /// Асинхронно удаляет выборку экземпляров сущности из БД, по переданной настройке.
    /// </summary>
    /// <remarks>Имеет эффект только после вызова <see cref="SaveChangesAsync"/>.</remarks>
    /// <param name="options">Настройки запроса. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task RemoveRangeAsync(ISpecification<TEntity> options);

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
