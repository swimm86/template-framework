// ----------------------------------------------------------------------------------------------
// <copyright file="PersonRepository.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Domain.Entities;
using Shared.Application.Core.Dal.Repository.Interfaces;
using Shared.Infrastructure.Dal.EFCore.Repository;

namespace Gpn.Template.Infrastructure.Dal.Repositories;

public class PersonRepository(
    DbContext dbContext,
    IQueryEvaluator evaluator)
    : EfRepository<Person>(dbContext, evaluator)
{
}
