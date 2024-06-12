// ----------------------------------------------------------------------------------------------
// <copyright file="PersonSpecificationRepository.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Domain.Entities;
using Shared.Application.Core.Dal.Repository.Interfaces;
using Shared.Infrastructure.Dal.EFCore.Specification;

namespace Gpn.Template.Infrastructure.Dal.Specifications;

/// <summary>
/// Репозиторий для Person.
/// </summary>
/// <param name="dbContext"><inheritdoc /></param>
/// <param name="evaluator"><inheritdoc /></param>
public class PersonSpecificationRepository(
    DbContext dbContext,
    IQueryEvaluator evaluator)
    : EfSpecificationRepository<Person>(dbContext, evaluator)
{
}
