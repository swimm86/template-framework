// ----------------------------------------------------------------------------------------------
// <copyright file="MapperProfile.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using AutoMapper;
using Template.Domain.Entities;
using Template.Getter.Application.Abstractions.Dto.Person.Responses;

namespace Template.Getter.Infrastructure.Mapping;

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
        CreateMap<Person, PersonListPayload>();
    }
}
