// ----------------------------------------------------------------------------------------------
// <copyright file="PersonRepository.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Infrastructure.Dal.EFCore.Repository;
using Template.Domain.Entities;

namespace Template.Infrastructure.Dal.Repositories;

/// <summary>
/// Репозиторий для Person.
/// </summary>
/// <param name="dbContext"><inheritdoc /></param>
/// <param name="evaluator"><inheritdoc /></param>
public class PersonRepository(
    DbContext dbContext,
    IQueryEvaluator evaluator)
    : EfRepository<Person>(dbContext, evaluator);
