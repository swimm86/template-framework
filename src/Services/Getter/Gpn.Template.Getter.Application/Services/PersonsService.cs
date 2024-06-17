// ----------------------------------------------------------------------------------------------
// <copyright file="PersonsService.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Domain.Entities;
using Gpn.Template.Getter.Application.Features.PersonFeature.Dtos.Requests;
using Gpn.Template.Getter.Application.Features.PersonFeature.Dtos.Responses;
using Gpn.Template.Getter.Application.Interfaces;
using Gpn.Template.Getter.Application.Specifications;
using Microsoft.AspNetCore.Http;
using Shared.Application.Core.Dal.Repository.Interfaces;
using Shared.Application.Core.Dal.Repository.Models;
using Shared.Application.Core.Dal.Specification.Interfaces;
using Shared.Application.Core.Dal.UnitOfWork.Interfaces;
using Shared.Application.Core.Dto.Responses;

namespace Gpn.Template.Getter.Application.Services;

/// <inheritdoc />
public class PersonsService(
    IUnitOfWork unitOfWork,
    IRepository<Person> personRepository,
    ISpecificationRepository<Person> personSpecification)
    : IPersonsService
{
    /// <inheritdoc />
    public async Task<PageableResponse<PersonDto>> GetPersonsAsync(
        GetPersonsRequestDto dto)
    {
        var personsTask = dto.DalPattern switch
        {
            DalPattern.UnitOfWork => GetPersonsUnitOfWorkAsync(),
            DalPattern.Repository => GetPersonsRepositoryAsync(),
            DalPattern.Specification => GetPersonsSpecificationAsync(),
            _ => throw new ArgumentOutOfRangeException()
        };
        var result = await personsTask.ConfigureAwait(false);
        var totalPages = dto.PageSize == 0 ? 0 : result.totalCount / dto.PageSize;
        return new PageableResponse<PersonDto>(totalPages, result.collection, StatusCodes.Status200OK);
    }

    /// <summary>
    /// Возвращает всех 'Person-ов' с использованием паттерна 'UnitOfWork'.
    /// </summary>
    /// <returns>Объект GetPersonsResponseDto, содержащий список всех 'Person-ов'.</returns>
    public Task<(ICollection<PersonDto> collection, int totalCount)> GetPersonsUnitOfWorkAsync()
    {
        return unitOfWork
            .ExecuteAsync<Person, (ICollection<PersonDto> collection, int totalCount)>(
                async repo =>
                {
                    var options = new QueryOptions<Person>();
                    var collection = await repo.GetRangeAsync<PersonDto>(options).ConfigureAwait(false);
                    var totalCount = await repo.CountAsync(options).ConfigureAwait(false);
                    return (collection, totalCount);
                },
                CancellationToken.None);
    }

    /// <summary>
    /// Возвращает всех 'Person-ов' с использованием паттерна 'Repository'.
    /// </summary>
    /// <returns>Объект GetPersonsResponseDto, содержащий список всех 'Person-ов'.</returns>
    private async Task<(ICollection<PersonDto> collection, int totalCount)> GetPersonsRepositoryAsync()
    {
        var options = new QueryOptions<Person>();
        var collection = await personRepository
            .GetRangeAsync<PersonDto>(options)
            .ConfigureAwait(false);
        var totalCount = await personRepository
            .CountAsync(options)
            .ConfigureAwait(false);
        return (collection, totalCount);
    }

    /// <summary>
    /// Возвращает всех 'Person-ов' с использованием паттерна 'Specification'.
    /// </summary>
    /// <returns>Объект GetPersonsResponseDto, содержащий список всех 'Person-ов'.</returns>
    private async Task<(ICollection<PersonDto> collection, int totalCount)> GetPersonsSpecificationAsync()
    {
        var specification = new PersonSpecification();
        var collection = await personSpecification
            .GetRangeAsync<PersonDto>(specification)
            .ConfigureAwait(false);
        var totalCount = await personSpecification
            .CountAsync(specification)
            .ConfigureAwait(false);
        return (collection, totalCount);
    }
}
