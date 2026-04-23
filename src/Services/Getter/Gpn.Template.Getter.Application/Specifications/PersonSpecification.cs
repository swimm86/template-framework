// ----------------------------------------------------------------------------------------------
// <copyright file="PersonSpecification.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Domain.Entities;
using Gpn.Template.Getter.Application.Abstractions.Dto.Person.Requests;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Dal.Specification.Models;

namespace Gpn.Template.Getter.Application.Specifications;

/// <summary>
/// Спецификация для 'Person'.
/// </summary>
public record PersonSpecification(PersonListRequest Request)
    : SpecificationBase<Person>(Request.ConvertSortOptions())
{
    /// <inheritdoc />
    public override QueryOptions<Person> BuildOptions()
    {
        base.BuildOptions();
        if (!string.IsNullOrWhiteSpace(Request.Filter?.Email))
        {
            Options.AddFilter(x => x.Email.ToLower().Equals(Request.Filter.Email.ToLower()));
        }

        return Options;
    }
}
