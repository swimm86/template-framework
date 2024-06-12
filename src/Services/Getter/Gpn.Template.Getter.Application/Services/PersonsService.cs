// ----------------------------------------------------------------------------------------------
// <copyright file="PersonsService.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Domain.Entities;
using Gpn.Template.Getter.Application.Interfaces;
using Shared.Application.Core.Dal.UnitOfWork.Interfaces;

namespace Gpn.Template.Getter.Application.Services;

/// <inheritdoc />
public class PersonsService(IUnitOfWork unitOfWork)
    : IPersonsService
{
    /// <inheritdoc />
    public List<Person> GetPersons()
    {
        return unitOfWork.Execute<Person, List<Person>>(r => r.Set().ToList());
    }
}
