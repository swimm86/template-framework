// ----------------------------------------------------------------------------------------------
// <copyright file="MapperProfile.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using AutoMapper;
using Gpn.Template.Domain.Entities;
using Gpn.Template.Getter.Application.Responses;

namespace Gpn.Template.Getter.Infrastructure.Mapping;

/// <summary>
/// Профиль маппинга.
/// </summary>
public class MapperProfile : Profile
{
    /// <summary>
    /// Конструктор класса. Содержит конфигурации маппингов.
    /// </summary>
    public MapperProfile()
    {
        CreateMap<Person, PersonDto>();
        CreateMap<ICollection<PersonDto>, GetPersonsResponseDto>()
            .ConstructUsing(persons => new GetPersonsResponseDto() { Persons = persons });
    }
}
