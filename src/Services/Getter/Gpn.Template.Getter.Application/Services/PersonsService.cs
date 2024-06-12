// ----------------------------------------------------------------------------------------------
// <copyright file="PersonsService.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Domain.Entities;
using Gpn.Template.Getter.Application.Interfaces;
using Gpn.Template.Getter.Application.Requests;
using Gpn.Template.Getter.Application.Responses;
using Microsoft.AspNetCore.Http;
using Shared.Application.Core.Dal.Repository.Interfaces;
using Shared.Application.Core.Dal.UnitOfWork.Interfaces;
using Shared.Application.Core.Dto.Responses;
using Shared.Application.Core.Mapping.Extensions;
using Shared.Application.Core.Mapping.Interfaces;

namespace Gpn.Template.Getter.Application.Services;

/// <inheritdoc />
public class PersonsService(
    IMapper mapper,
    IUnitOfWork unitOfWork,
    IRepository<Person> personRepository)
    : IPersonsService
{
    /// <inheritdoc />
    public Response<GetPersonsResponseDto> GetPersons(GetPersonsRequestDto dto)
    {
        return new Response<GetPersonsResponseDto>(dto.DalPattern switch
        {
            DalPattern.UnitOfWork => GetPersonsUnitOfWork(),
            DalPattern.Repository => GetPersonsRepository(),
            DalPattern.Specification => GetPersonsSpecification(),
            _ => throw new ArgumentOutOfRangeException()
        }, StatusCodes.Status200OK);
    }

    /// <summary>
    /// Возвращает всех 'Person-ов' с использованием паттерна 'UnitOfWork'.
    /// </summary>
    /// <returns>Объект GetPersonsResponseDto, содержащий список всех 'Person-ов'.</returns>
    public GetPersonsResponseDto GetPersonsUnitOfWork()
    {
        unitOfWork
            .Execute<Person, List<PersonDto>>(r =>
                r.Set().ProjectTo<PersonDto>(mapper).ToList());

        return mapper
            .Map<List<PersonDto>, GetPersonsResponseDto>(
                unitOfWork
                    .Execute<Person, List<PersonDto>>(r =>
                        r.Set().ProjectTo<PersonDto>(mapper).ToList()));
    }

    /// <summary>
    /// Возвращает всех 'Person-ов' с использованием паттерна 'Repository'.
    /// </summary>
    /// <returns>Объект GetPersonsResponseDto, содержащий список всех 'Person-ов'.</returns>
    private GetPersonsResponseDto GetPersonsRepository()
    {
        return mapper
            .Map<List<PersonDto>, GetPersonsResponseDto>(
                personRepository.Set().ProjectTo<PersonDto>(mapper).ToList());
    }

    /// <summary>
    /// Возвращает всех 'Person-ов' с использованием паттерна 'Specification'.
    /// </summary>
    /// <returns>Объект GetPersonsResponseDto, содержащий список всех 'Person-ов'.</returns>
    public GetPersonsResponseDto GetPersonsSpecification()
    {
        throw new NotImplementedException();
    }
}
