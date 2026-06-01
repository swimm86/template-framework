// ----------------------------------------------------------------------------------------------
// <copyright file="MapperProfile.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using AutoMapper;
using Template.Application.Dto.Person;
using Template.Domain.Entities;

namespace Template.Infrastructure.Mapping;

/// <summary>
/// Профиль маппинга.
/// </summary>
public class MapperProfile
    : Profile
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="MapperProfile"/>. Содержит конфигурации маппингов.
    /// </summary>
    public MapperProfile()
    {
        CreateMap<PersonDto, Person>()
            .ForMember(
                dest => dest.Id,
                opt => opt.MapFrom(src => src.Id))
            .ForMember(
                dest => dest.Name,
                opt => opt.MapFrom(src => src.Name))
            .ForMember(
                dest => dest.Email,
                opt => opt.MapFrom(src => src.Email));

        CreateMap<Person, PersonDto>()
            .ForMember(
                dest => dest.Id,
                opt => opt.MapFrom(src => src.Id))
            .ForMember(
                dest => dest.Name,
                opt => opt.MapFrom(src => src.Name))
            .ForMember(
                dest => dest.Email,
                opt => opt.MapFrom(src => src.Email));
    }
}
