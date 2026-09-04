// ----------------------------------------------------------------------------------------------
// <copyright file="MapperProfile.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Mapping;
using Template.Application.Dto.Person;
using Template.Domain.Entities;

namespace Template.Application.Mapping;

/// <summary>
/// Профиль маппинга.
/// </summary>
public class MapperProfile
    : MappingProfileBase
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="MapperProfile"/> и регистрирует конфигурации маппингов.
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
