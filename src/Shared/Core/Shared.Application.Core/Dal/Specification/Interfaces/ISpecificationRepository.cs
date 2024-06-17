// ----------------------------------------------------------------------------------------------
// <copyright file="ISpecificationRepository.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.Dal.Specification.Interfaces;

/// <summary>
/// Интерфейс, предоставляющий репозиторий данных с использованием спецификаций.
/// </summary>
/// <typeparam name="TEntity">Тип сущности.</typeparam>
public interface ISpecificationRepository<TEntity>
    : IRepository<TEntity>
    where TEntity : class, IEntity
{
    #region Read methods

    /// <summary>
    /// Асинхронно возвращает экземпляр сущности по ее индентификатору.
    /// </summary>
    /// <param name="id">Идентификатор сущности.</param>
    /// <param name="specification">Спецификация. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <returns>Экземпляр сущности, если найден, иначе null.</returns>
    Task<TEntity?> GetAsync(
        object id,
        ISpecification<TEntity>? specification = null) =>
        GetAsync(id, specification?.BuildOptions());

    /// <summary>
    /// Асинхронно возвращает коллекцию экземпляров сущности по переданной настройке.
    /// </summary>
    /// <param name="specification">Спецификация. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
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
    /// <param name="specification">Спецификация. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
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
    /// <param name="specification">Спецификация. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <returns>Первый попавшийся экземпляр сущности из выборки по переданной спецификации, если выборка не пуста, иначе null.</returns>
    Task<TEntity?> FirstOrDefaultAsync(ISpecification<TEntity> specification) =>
        FirstOrDefaultAsync(specification.BuildOptions());

    /// <summary>
    /// Асинхронно возвращает единственный экземпляр сущности из выборки по переданной настройке.
    /// </summary>
    /// <exception cref="T:System.InvalidOperationException">Выборка по спецификации содержит более одного элемента.</exception>
    /// <param name="specification">Спецификация. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <returns>Единственный экземпляр сущности из выборки по переданной спецификации, если выборка имеет 1 элемент, иначе null.</returns>
    Task<TEntity?> SingleOrDefaultAsync(ISpecification<TEntity> specification) =>
        SingleOrDefaultAsync(specification.BuildOptions());

    /// <summary>
    /// Асинхронно возвращает последний экземпляр сущности из выборки по переданной настройке.
    /// </summary>
    /// <param name="specification">Спецификация. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <returns>Последний экземпляр сущности из выборки по переданной спецификации, если выборка не пуста, иначе null.</returns>
    Task<TEntity?> LastOrDefaultAsync(ISpecification<TEntity> specification) =>
        LastOrDefaultAsync(specification.BuildOptions());

    #endregion

    #region Utility methods

    /// <summary>
    /// Асинхронно возвращает количество элементов в выборке по настройке.
    /// </summary>
    /// <param name="specification">Спецификация. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <returns>Количество элементов в выборке по спецификации.</returns>
    Task<int> CountAsync(ISpecification<TEntity> specification) =>
        CountAsync(specification.BuildOptions());

    #endregion

    #region Remove methods

    /// <summary>
    /// Асинхронно удаляет выборку экземпляров сущности из БД, по переданной настройке.
    /// </summary>
    /// <remarks>Имеет эффект только после вызова <see cref="SaveChangesAsync"/>.</remarks>
    /// <param name="specification">Спецификация. Если параметр равен null, запрос будет выполнен без применения дополнительных настроек.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task RemoveRangeAsync(ISpecification<TEntity> specification) =>
        RemoveRangeAsync(specification.BuildOptions());

    #endregion
}
