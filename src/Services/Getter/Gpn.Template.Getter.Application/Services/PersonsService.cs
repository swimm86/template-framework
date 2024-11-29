// ----------------------------------------------------------------------------------------------
// <copyright file="PersonsService.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Domain.Entities;
using Gpn.Template.Getter.Application.Abstractions.Dto.Person.Requests;
using Gpn.Template.Getter.Application.Abstractions.Dto.Person.Responses;
using Gpn.Template.Getter.Application.Abstractions.Enums;
using Gpn.Template.Getter.Application.Interfaces;
using Gpn.Template.Getter.Application.Specifications;
using Microsoft.AspNetCore.Http;
using Shared.Application.Core.Dal.Repository.Interfaces;
using Shared.Application.Core.Dal.Repository.Models;
using Shared.Application.Core.Dal.UnitOfWork.Interfaces;

namespace Gpn.Template.Getter.Application.Services;

/// <inheritdoc />
public class PersonsService(
    IUnitOfWork unitOfWork,
    IRepository<Person> personRepository)
    : IPersonsService
{
    /// <inheritdoc />
    public async Task<PersonListResponse> GetPersonsAsync(
        PersonListRequest request)
    {
        var skip = request.PageSize * request.PageNumber;
        var take = request.PageSize;
        var personsTask = request.DalPattern switch
        {
            DalPattern.UnitOfWork => GetPersonsUnitOfWorkAsync(skip, take),
            DalPattern.Repository => GetPersonsRepositoryAsync(skip, take),
            DalPattern.Specification => GetPersonsSpecificationAsync(skip, take),
            _ => throw new ArgumentOutOfRangeException()
        };
        var result = await personsTask.ConfigureAwait(false);
        var totalPages = request.PageSize == 0 ? 0 : result.totalCount / request.PageSize;
        var status = result.totalCount > 0 ? StatusCodes.Status200OK : StatusCodes.Status204NoContent;
        return new PersonListResponse(totalPages, result.collection, status);
    }

    /// <summary>
    /// Возвращает всех 'Person-ов' с использованием паттерна 'UnitOfWork'.
    /// </summary>
    /// <param name="skip">Количество сущностей, которые необходимо пропустить.</param>
    /// <param name="take">Количество сущностей, которые необходимо извлечь.</param>
    /// <returns>Объект GetPersonsResponseDto, содержащий список всех 'Person-ов'.</returns>
    public Task<(ICollection<PersonListPayload> collection, int totalCount)> GetPersonsUnitOfWorkAsync(int skip, int take)
    {
        return unitOfWork
            .ExecuteAsync<Person, (ICollection<PersonListPayload> collection, int totalCount)>(
                async repo =>
                {
                    var options = new QueryOptions<Person>();
                    var collection = await repo.GetRangeAsync<PersonListPayload>(options, skip, take).ConfigureAwait(false);
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
    private async Task<(ICollection<PersonListPayload> collection, int totalCount)> GetPersonsRepositoryAsync(int skip, int take)
    {
        var options = new QueryOptions<Person>();
        var collection = await personRepository
            .GetRangeAsync<PersonListPayload>(options, skip, take)
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
    private async Task<(ICollection<PersonListPayload> collection, int totalCount)> GetPersonsSpecificationAsync(int skip, int take)
    {
        var specification = new PersonSpecification();
        var collection = await personRepository
            .GetRangeAsync<PersonListPayload>(specification, skip, take)
            .ConfigureAwait(false);
        var totalCount = await personRepository
            .CountAsync(specification)
            .ConfigureAwait(false);
        return (collection, totalCount);
    }
}
