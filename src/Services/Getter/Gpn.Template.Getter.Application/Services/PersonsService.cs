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
using Shared.Application.Core.Dal.Repository.Interfaces;
using Shared.Application.Core.Dal.Repository.Models;
using Shared.Application.Core.Dal.UnitOfWork.Interfaces;
using Shared.Application.Core.Dto.Responses;

namespace Gpn.Template.Getter.Application.Services;

/// <inheritdoc />
public class PersonsService(
    IUnitOfWork unitOfWork,
    IRepository<Person> personRepository)
    : IPersonsService
{
    /// <inheritdoc />
    public async Task<PageableResponse<ICollection<PersonDto>>> GetPersonsAsync(
        GetPersonsRequestDto dto)
    {
        var skip = dto.PageSize * dto.PageNumber;
        var take = dto.PageSize;
        var personsTask = dto.DalPattern switch
        {
            DalPattern.UnitOfWork => GetPersonsUnitOfWorkAsync(skip, take),
            DalPattern.Repository => GetPersonsRepositoryAsync(skip, take),
            DalPattern.Specification => GetPersonsSpecificationAsync(skip, take),
            _ => throw new ArgumentOutOfRangeException()
        };
        var result = await personsTask.ConfigureAwait(false);
        var totalPages = dto.PageSize == 0 ? 0 : result.totalCount / dto.PageSize;
        return new PageableResponse<ICollection<PersonDto>>(totalPages, result.collection);
    }

    /// <summary>
    /// Возвращает всех 'Person-ов' с использованием паттерна 'UnitOfWork'.
    /// </summary>
    /// <param name="skip">Количество сущностей, которые необходимо пропустить.</param>
    /// <param name="take">Количество сущностей, которые необходимо извлечь.</param>
    /// <returns>Объект GetPersonsResponseDto, содержащий список всех 'Person-ов'.</returns>
    public Task<(ICollection<PersonDto> collection, int totalCount)> GetPersonsUnitOfWorkAsync(int skip, int take)
    {
        return unitOfWork
            .ExecuteAsync<Person, (ICollection<PersonDto> collection, int totalCount)>(
                async repo =>
                {
                    var options = new QueryOptions<Person>();
                    var collection = await repo.GetRangeAsync<PersonDto>(options, skip, take).ConfigureAwait(false);
                    var totalCount = await repo.CountAsync(options).ConfigureAwait(false);
                    return (collection, totalCount);
                },
                CancellationToken.None);
    }

    /// <summary>
    /// Возвращает всех 'Person-ов' с использованием паттерна 'Repository'.
    /// </summary>
    /// <param name="skip">Количество сущностей, которые необходимо пропустить.</param>
    /// <param name="take">Количество сущностей, которые необходимо извлечь.</param>
    /// <returns>Объект GetPersonsResponseDto, содержащий список всех 'Person-ов'.</returns>
    private async Task<(ICollection<PersonDto> collection, int totalCount)> GetPersonsRepositoryAsync(int skip, int take)
    {
        var options = new QueryOptions<Person>();
        var collection = await personRepository
            .GetRangeAsync<PersonDto>(options, skip, take)
            .ConfigureAwait(false);
        var totalCount = await personRepository
            .CountAsync(options)
            .ConfigureAwait(false);
        return (collection, totalCount);
    }

    /// <summary>
    /// Возвращает всех 'Person-ов' с использованием паттерна 'Specification'.
    /// </summary>
    /// <param name="skip">Количество сущностей, которые необходимо пропустить.</param>
    /// <param name="take">Количество сущностей, которые необходимо извлечь.</param>
    /// <returns>Объект GetPersonsResponseDto, содержащий список всех 'Person-ов'.</returns>
    private async Task<(ICollection<PersonDto> collection, int totalCount)> GetPersonsSpecificationAsync(int skip, int take)
    {
        var specification = new PersonSpecification();
        var collection = await personRepository
            .GetRangeAsync<PersonDto>(specification, skip, take)
            .ConfigureAwait(false);
        var totalCount = await personRepository
            .CountAsync(specification)
            .ConfigureAwait(false);
        return (collection, totalCount);
    }
}
