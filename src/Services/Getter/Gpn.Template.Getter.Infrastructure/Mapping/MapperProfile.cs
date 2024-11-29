// ----------------------------------------------------------------------------------------------
// <copyright file="MapperProfile.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using AutoMapper;
using Gpn.Template.Domain.Entities;
using Gpn.Template.Getter.Application.Abstractions.Dto.Person.Responses;

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
        CreateMap<Person, PersonListPayload>();
    }
}
