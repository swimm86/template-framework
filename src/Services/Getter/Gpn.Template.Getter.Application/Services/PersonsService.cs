// ----------------------------------------------------------------------------------------------
// <copyright file="PersonsService.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Domain.Entities;
using Gpn.Template.Getter.Application.Abstractions.Dto.Person.Requests;
using Gpn.Template.Getter.Application.Abstractions.Dto.Person.Responses;
using Gpn.Template.Getter.Application.Abstractions.Enums;
using Gpn.Template.Getter.Application.Interfaces;
using Gpn.Template.Getter.Application.Specifications;
using Microsoft.AspNetCore.Http;
using Shared.Common.Extensions;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Domain.Core.Dal.Repository.Models;
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
        PersonListRequest request,
        CancellationToken cancellationToken)
    {
        var skip = request.PageSize * request.PageNumber;
        var take = request.PageSize;
        var personsTask = request.DalPattern switch
        {
            DalPattern.UnitOfWork => GetPersonsUnitOfWorkAsync(request, skip, take, cancellationToken),
            DalPattern.Repository => GetPersonsRepositoryAsync(request, skip, take, cancellationToken),
            DalPattern.Specification => GetPersonsSpecificationAsync(request, skip, take, cancellationToken),
            _ => throw new ArgumentOutOfRangeException()
        };
        var result = await personsTask;
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
    /// <param name="request">Запрос.</param>
    /// <param name="skip">Количество сущностей, которые необходимо пропустить.</param>
    /// <param name="take">Количество сущностей, которые необходимо извлечь.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns>Объект GetPersonsResponseDto, содержащий список всех 'Person-ов'.</returns>
    private async Task<(ICollection<PersonListPayload> collection, int totalCount)> GetPersonsUnitOfWorkAsync(
        PersonListRequest request,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var options = new QueryOptions<Person>();
        request.ConvertSortOptions().ForEach(options.AddOrderBy);
        var repo = unitOfWork.GetRepository<Person>();
        var collection = await repo.GetRangeAsync<PersonListPayload>(
            skip: skip,
            take: take,
            cancellationToken: cancellationToken);
        var totalCount = await repo.CountAsync(cancellationToken: cancellationToken);
        return (collection, totalCount);
    }

    /// <summary>
    /// Возвращает всех 'Person-ов' с использованием паттерна 'Repository'.
    /// </summary>
    /// <param name="request">Запрос.</param>
    /// <param name="skip">Количество сущностей, которые необходимо пропустить.</param>
    /// <param name="take">Количество сущностей, которые необходимо извлечь.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns>Объект GetPersonsResponseDto, содержащий список всех 'Person-ов'.</returns>
    private async Task<(ICollection<PersonListPayload> collection, int totalCount)> GetPersonsRepositoryAsync(
        PersonListRequest request,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var options = new QueryOptions<Person>();
        request.ConvertSortOptions().ForEach(options.AddOrderBy);
        var collection = await personRepository
            .GetRangeAsync<PersonListPayload>(
                options,
                skip,
                take,
                cancellationToken: cancellationToken);
        var totalCount = await personRepository
            .CountAsync(options, cancellationToken);
        return (collection, totalCount);
    }

    /// <summary>
    /// Возвращает всех 'Person-ов' с использованием паттерна 'Specification'.
    /// </summary>
    /// <param name="request">Запрос.</param>
    /// <param name="skip">Количество сущностей, которые необходимо пропустить.</param>
    /// <param name="take">Количество сущностей, которые необходимо извлечь.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns>Объект GetPersonsResponseDto, содержащий список всех 'Person-ов'.</returns>
    private async Task<(ICollection<PersonListPayload> collection, int totalCount)> GetPersonsSpecificationAsync(
        PersonListRequest request,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var specification = new PersonSpecification(request);
        var collection = await personRepository
            .GetRangeAsync<PersonListPayload>(
                specification,
                skip,
                take,
                cancellationToken: cancellationToken);
        var totalCount = await personRepository
            .CountAsync(specification, cancellationToken);
        return (collection, totalCount);
    }
}
