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
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Dal.Repository.Models;
using Shared.Domain.Core.Dal.Specification.Interfaces;
using Shared.Domain.Core.Dal.UnitOfWork.Interfaces;

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
        return new PersonListResponse
        {
            PageNumber = request.PageNumber,
            Payload = result.collection,
            StatusCode = status,
            TotalPages = totalPages,
        };
    }

    /// <summary>
    /// Возвращает всех 'Person-ов' с использованием паттерна 'UnitOfWork'.
    /// </summary>
    /// <param name="skip">Количество сущностей, которые необходимо пропустить.</param>
    /// <param name="take">Количество сущностей, которые необходимо извлечь.</param>
    /// <returns>Объект GetPersonsResponseDto, содержащий список всех 'Person-ов'.</returns>
    public async Task<(ICollection<PersonListPayload> collection, int totalCount)> GetPersonsUnitOfWorkAsync(
        int skip,
        int take)
    {
        var options = new QueryOptions<Person>();

        var optionsWithList = new QueryOptions<Person>();

        optionsWithList.AddFilter(person => person.Name == "Niki" || person.Name == "Nikita");

        optionsWithList
            .AddInclude(person => person.PersonWorks)
            .ThenInclude(personWork => personWork.Work)
            .AddInclude(person => person.OneToOne);

        var optionsWithIOrderedEnumerable = new QueryOptions<Person>();

        optionsWithIOrderedEnumerable.AddFilter(person => person.Name == "Niki" || person.Name == "Nikita");

        optionsWithIOrderedEnumerable
            .AddInclude(person => person.PersonWorks.OrderBy(personWork => personWork.Id))
            .ThenInclude(personWork => personWork.Work);

        var optionsWithIEnumerable = new QueryOptions<Person>();

        optionsWithIEnumerable.AddFilter(person => person.Name == "Niki" || person.Name == "Nikita");

        optionsWithIEnumerable
            .AddInclude(person => person.PersonWorks.Where(personWork => personWork.Person.Name == "Nikita"))
            .ThenInclude(personWork => personWork.Work);

        var repo = unitOfWork.GetRepository<Person>();

        var resultWithList = await repo.GetRangeAsync(options: optionsWithList, skip: skip, take: 200);
        var resultWithIEnumerable = await repo.GetRangeAsync(options: optionsWithIEnumerable, skip: skip, take: 200);
        var resultWithOrderedEnumerable = await repo.GetRangeAsync(options: optionsWithIOrderedEnumerable, skip: skip, take: 200);

        var collection = await repo.GetRangeAsync<PersonListPayload>(options: options, skip: skip, take: take);
        var totalCount = await repo.CountAsync();

        return (collection, totalCount);
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
            .GetRangeAsync<PersonListPayload>(specification, skip, take);
        var totalCount = await personRepository
            .CountAsync(specification);
        return (collection, totalCount);
    }
}
