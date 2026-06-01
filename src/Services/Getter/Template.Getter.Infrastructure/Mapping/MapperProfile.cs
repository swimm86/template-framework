// ----------------------------------------------------------------------------------------------
// <copyright file="MapperProfile.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using AutoMapper;
using Template.Application.Dto.Person;
using Template.Domain.Entities;
using Template.Getter.Application.Abstractions.Features.Person.List.Response;

namespace Template.Getter.Infrastructure.Mapping;

/// <summary>
/// Профиль маппинга.
/// </summary>
public class MapperProfile : Profile
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="MapperProfile"/>. Содержит конфигурации маппингов.
    /// </summary>
    public MapperProfile()
    {
        CreateMap<Person, PersonListPayload>()
            .IncludeBase<Person, PersonDto>();
    }
}
