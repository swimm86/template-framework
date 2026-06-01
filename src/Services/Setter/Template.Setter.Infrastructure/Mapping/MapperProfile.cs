// ----------------------------------------------------------------------------------------------
// <copyright file="MapperProfile.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using AutoMapper;
using Template.Domain.Entities;
using Template.Setter.Application.Abstractions.Features.Person.Create.Request;

namespace Template.Setter.Infrastructure.Mapping;

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
        CreateMap<PersonCreateRequest, Person>()
            .ConstructUsing(src => Person.Create(src.Name, src.Email));
    }
}
