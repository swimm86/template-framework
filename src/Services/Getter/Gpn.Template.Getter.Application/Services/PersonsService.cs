// ----------------------------------------------------------------------------------------------
// <copyright file="PersonsService.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Domain.Entities;
using Gpn.Template.Getter.Application.Interfaces;
using Gpn.Template.Getter.Application.Requests;
using Gpn.Template.Getter.Application.Responses;
using Gpn.Template.Getter.Application.Specifications;
using Microsoft.AspNetCore.Http;
using Shared.Application.Core.Dal.Repository.Interfaces;
using Shared.Application.Core.Dal.Repository.Models;
using Shared.Application.Core.Dal.Specification.Interfaces;
using Shared.Application.Core.Dal.UnitOfWork.Interfaces;
using Shared.Application.Core.Dto.Responses;
using Shared.Application.Core.Mapping.Interfaces;

namespace Gpn.Template.Getter.Application.Services;

/// <inheritdoc />
public class PersonsService(
    IMapper mapper,
    IUnitOfWork unitOfWork,
    IRepository<Person> personRepository,
    ISpecificationRepository<Person> personSpecification)
    : IPersonsService
{
    /// <inheritdoc />
    public async Task<Response<GetPersonsResponseDto>> GetPersonsAsync(
        GetPersonsRequestDto dto)
    {
        var result = dto.DalPattern switch
        {
            DalPattern.UnitOfWork => GetPersonsUnitOfWorkAsync(),
            DalPattern.Repository => GetPersonsRepositoryAsync(),
            DalPattern.Specification => GetPersonsSpecificationAsync(),
            _ => throw new ArgumentOutOfRangeException()
        };
        return new Response<GetPersonsResponseDto>(await result.ConfigureAwait(false), StatusCodes.Status200OK);
    }

    /// <summary>
    /// Возвращает всех 'Person-ов' с использованием паттерна 'UnitOfWork'.
    /// </summary>
    /// <returns>Объект GetPersonsResponseDto, содержащий список всех 'Person-ов'.</returns>
    public async Task<GetPersonsResponseDto> GetPersonsUnitOfWorkAsync()
    {
        return mapper
            .Map<List<PersonDto>, GetPersonsResponseDto>(
                await unitOfWork
                    .ExecuteAsync<Person, List<PersonDto>>(
                        repo =>
                            repo.GetRangeAsync<PersonDto>(new QueryOptions<Person>()),
                        CancellationToken.None)
                    .ConfigureAwait(false));
    }

    /// <summary>
    /// Возвращает всех 'Person-ов' с использованием паттерна 'Repository'.
    /// </summary>
    /// <returns>Объект GetPersonsResponseDto, содержащий список всех 'Person-ов'.</returns>
    private async Task<GetPersonsResponseDto> GetPersonsRepositoryAsync()
    {
        return mapper
            .Map<List<PersonDto>, GetPersonsResponseDto>(
                await personRepository.GetRangeAsync<PersonDto>(new QueryOptions<Person>())
                    .ConfigureAwait(false));
    }

    /// <summary>
    /// Возвращает всех 'Person-ов' с использованием паттерна 'Specification'.
    /// </summary>
    /// <returns>Объект GetPersonsResponseDto, содержащий список всех 'Person-ов'.</returns>
    private async Task<GetPersonsResponseDto> GetPersonsSpecificationAsync()
    {
        return mapper
            .Map<List<PersonDto>, GetPersonsResponseDto>(
                await personSpecification.GetRangeAsync<PersonDto>(new PersonSpecification())
                    .ConfigureAwait(false));
    }
}
