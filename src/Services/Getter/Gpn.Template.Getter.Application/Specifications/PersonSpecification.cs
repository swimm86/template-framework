// ----------------------------------------------------------------------------------------------
// <copyright file="PersonSpecification.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Domain.Entities;
using Shared.Application.Core.Dal.Repository.Models;
using Shared.Application.Core.Dal.Specification.Models;

namespace Gpn.Template.Getter.Application.Specifications;

/// <summary>
/// Спецификация для 'Person'.
/// </summary>
public record PersonSpecification : SpecificationBase<Person>
{
    /// <inheritdoc />
    public override QueryOptions<Person> BuildOptions()
    {
        return new QueryOptions<Person>();
    }
}
