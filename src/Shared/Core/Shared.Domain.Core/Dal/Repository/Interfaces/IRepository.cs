// ----------------------------------------------------------------------------------------------
// <copyright file="IRepository.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Interfaces;

namespace Shared.Domain.Core.Dal.Repository.Interfaces;

/// <summary>
/// Интерфейс репозитория данных для сущности <typeparamref name="TEntity"/>,
/// объединяющий контракты чтения (<see cref="IGetterRepository{TEntity}"/>) и модификации (<see cref="ISetterRepository{TEntity}"/>).
/// </summary>
/// <typeparam name="TEntity">Тип сущности.</typeparam>
/// <remarks>
/// <para>Контракт репозитория разделён на три интерфейса в духе Interface Segregation Principle:</para>
/// <list type="bullet">
///   <item><see cref="IGetterRepository{TEntity}"/> — read-only: запросы, проекции, агрегации.</item>
///   <item><see cref="ISetterRepository{TEntity}"/> — write-only: добавление, обновление, удаление, Execute, SaveChanges.</item>
///   <item><see cref="IRepository{TEntity}"/> — composite: оба контракта плюс <see cref="Set(QueryOptions{TEntity}?)"/>.</item>
/// </list>
/// </remarks>
public interface IRepository<TEntity>
    : IGetterRepository<TEntity>, ISetterRepository<TEntity>
    where TEntity : class, IEntity
{
    /// <summary>
    /// Возвращает <see cref="IQueryable{TEntity}"/> для построения запросов.
    /// </summary>
    /// <param name="options">Настройки запроса. Если <see langword="null"/>, запрос выполняется без дополнительных настроек.</param>
    /// <returns>Запрос <see cref="IQueryable{TEntity}"/> с применёнными настройками.</returns>
    IQueryable<TEntity> Set(QueryOptions<TEntity>? options = null);
}
