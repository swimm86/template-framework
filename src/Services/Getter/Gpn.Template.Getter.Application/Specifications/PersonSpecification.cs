// ----------------------------------------------------------------------------------------------
// <copyright file="PersonSpecification.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
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
public record PersonSpecification(
    PersonListRequest Request,
    QueryOptions<Person>? Options = default)
    : SpecificationBase<Person>(
        Options,
        Request.ConvertSortOptions(),
        Request.Filter?.Fields)
{
    /// <inheritdoc />
    public override QueryOptions<Person> BuildOptions()
    {
        if (!string.IsNullOrWhiteSpace(Request.Filter?.Email))
        {
            Options.AddFilter(x => x.Email.ToLower().Equals(Request.Filter.Email.ToLower()));
        }

        return Options;
    }
}
