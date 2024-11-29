// ----------------------------------------------------------------------------------------------
// <copyright file="PersonRepository.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Domain.Entities;
using Shared.Application.Core.Dal.Repository.Interfaces;
using Shared.Infrastructure.Dal.EFCore.Repository;

namespace Gpn.Template.Infrastructure.Dal.Repositories;

/// <summary>
/// Репозиторий для Person.
/// </summary>
/// <param name="dbContext"><inheritdoc /></param>
/// <param name="evaluator"><inheritdoc /></param>
public class PersonRepository(
    DbContext dbContext,
    IQueryEvaluator evaluator)
    : EfRepository<Person>(dbContext, evaluator)
{
}
