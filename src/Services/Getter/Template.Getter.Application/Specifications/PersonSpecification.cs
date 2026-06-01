// ----------------------------------------------------------------------------------------------
// <copyright file="PersonSpecification.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Dal.Specification.Models;
using Template.Domain.Entities;
using Template.Getter.Application.Abstractions.Features.Person.List.Request;

namespace Template.Getter.Application.Specifications;

/// <summary>
/// Спецификация для "Персона".
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
