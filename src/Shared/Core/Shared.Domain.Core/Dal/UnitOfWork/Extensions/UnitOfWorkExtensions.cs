// ----------------------------------------------------------------------------------------------
// <copyright file="UnitOfWorkExtensions.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Linq.Expressions;
using System.Reflection;
using Shared.Domain.Core.Attributes;
using Shared.Domain.Core.Dal.Repository.Extensions;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;
using Shared.Domain.Core.Exceptions.Models;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Dal.UnitOfWork.Extensions;

/// <summary>
/// Расширение для <see cref="IUnitOfWork"/>.
/// </summary>
public static class UnitOfWorkExtensions
{
    /// <summary>
    /// Проверка на существование сущности с заданным именем.
    /// Если сущность с заданным именем уже существует, то:
    /// если она удалена, то она удаляется физически;
    /// если нет - то выбрасывается исключение.
    /// </summary>
    /// <typeparam name="TEntity">Сущность.</typeparam>
    /// <param name="unitOfWork"><see cref="IUnitOfWork"/>.</param>
    /// <param name="nameFilter">Фильтр по параметру для сравнения.</param>
    /// <param name="nameSelector">Функция для получения имени сущности.</param>
    /// <param name="data">Идентификатор сущности.</param>
    /// <returns>Результат проверки.</returns>
    /// <exception cref="BusinessLogicException">Искомая сущность уже существует.</exception>
    public static Task EnsureUniqueEntityByNameAsync<TEntity>(
        this IUnitOfWork unitOfWork,
        Expression<Func<TEntity, bool>> nameFilter,
        Func<TEntity, string> nameSelector,
        params (string name, Guid? id)[] data)
        where TEntity : class, IEntity<Guid>
    {
        var type = typeof(TEntity);
        var entityName =
            type.GetCustomAttribute<EntityNameAttribute>()?.Name ??
            type.Name;
        var comparableParameterName =
            type.GetCustomAttribute<EntityComparableNameAttribute>()?.Name ??
            EntityComparableNameAttribute.DefaultComparableParameterName;
        return EnsureUniqueEntityByNameAsync(
            unitOfWork,
            nameFilter,
            nameSelector,
            entityName,
            comparableParameterName,
            data);
    }

    /// <summary>
    /// Проверка на существование сущности с заданным именем.
    /// Если сущность с заданным именем уже существует, то:
    /// если она удалена, то она удаляется физически;
    /// если нет - то выбрасывается исключение.
    /// </summary>
    /// <typeparam name="TEntity">Сущность.</typeparam>
    /// <param name="unitOfWork"><see cref="IUnitOfWork"/>.</param>
    /// <param name="nameFilter">Фильтр по параметру для сравнения.</param>
    /// <param name="nameSelector">Функция для получения имени сущности.</param>
    /// <param name="entityName">Название сущности.</param>
    /// <param name="comparableParameterName">Название параметра сравнения.</param>
    /// <param name="data">Идентификатор сущности.</param>
    /// <returns>Результат проверки.</returns>
    /// <exception cref="BusinessLogicException">Искомая сущность уже существует.</exception>
    public static async Task EnsureUniqueEntityByNameAsync<TEntity>(
        this IUnitOfWork unitOfWork,
        Expression<Func<TEntity, bool>> nameFilter,
        Func<TEntity, string> nameSelector,
        string entityName,
        string comparableParameterName,
        params (string name, Guid? id)[] data)
        where TEntity : class, IEntity<Guid>
    {
        var repository = unitOfWork.GetRepository<TEntity>();
        var existed = await repository.FindEntitiesByNamesAsync(
            data.Select(x => x.id).OfType<Guid>().ToArray(),
            nameFilter);
        foreach (var entity in existed)
        {
            await unitOfWork.HandleDuplicateEntityAsync(
                entity,
                entityName,
                comparableParameterName,
                nameSelector(entity));
        }
    }

    /// <summary>
    /// Обрабатывает дублирующуюся сущность.
    /// </summary>
    /// <typeparam name="TEntity">Сущность.</typeparam>
    /// <param name="unitOfWork"><see cref="IUnitOfWork"/>.</param>
    /// <param name="existed">Существующая сущность.</param>
    /// <param name="entityName">Название сущности.</param>
    /// <param name="comparableParameterName">Название параметра сравнения.</param>
    /// <param name="name">Параметр сравнения.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    /// <exception cref="BusinessLogicException">Искомая сущность уже существует.</exception>
    private static Task HandleDuplicateEntityAsync<TEntity>(
        this IUnitOfWork unitOfWork,
        TEntity? existed,
        string? entityName,
        string comparableParameterName,
        string name)
        where TEntity : class, IEntity<Guid>
    {
        if (existed == null)
        {
            return Task.CompletedTask;
        }

        if (existed is not IDeletable { IsDeleted: true })
        {
            throw new BusinessLogicException($"{entityName} с {comparableParameterName} '{name}' уже существует.");
        }

        if (existed is IWithDeleteAction<TEntity> entityWithDeleteAction)
        {
            return entityWithDeleteAction.DeleteAsync(unitOfWork, false);
        }

        var repo = unitOfWork.GetRepository<TEntity>();
        return repo.RemoveAsync(existed, true);
    }
}
