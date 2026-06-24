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
/// <para>Рекомендации по выбору интерфейса репозитория:</para>
/// <list type="bullet">
///   <item><see cref="IGetterRepository{TEntity}"/> — для read-only сценариев (Getter-сервис, проекции, агрегации).</item>
///   <item><see cref="ISetterRepository{TEntity}"/> — для write-only сценариев (Setter-сервис, команды).</item>
///   <item><see cref="IRepository{TEntity}"/> — когда в рамках одного scope требуются и чтение, и запись.</item>
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
