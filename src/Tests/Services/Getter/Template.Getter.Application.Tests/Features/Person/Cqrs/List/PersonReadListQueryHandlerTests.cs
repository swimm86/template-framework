// ----------------------------------------------------------------------------------------------
// <copyright file="PersonReadListQueryHandlerTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging.Abstractions;
using Shared.Application.Core.Dto.Requests;
using Template.Getter.Application.Abstractions.Enums;
using Template.Getter.Application.Abstractions.Features.Person.List.Request;
using Template.Getter.Application.Abstractions.Features.Person.List.Response;
using Template.Getter.Application.Features.Person.Cqrs.List;
using Template.Getter.Application.Tests.TestDoubles;
using PersonEntity = Template.Domain.Entities.Person;

namespace Template.Getter.Application.Tests.Features.Person.Cqrs.List;

/// <summary>
/// Тесты <see cref="PersonReadListQueryHandler"/>.
/// Проверяют пагинацию, фильтрацию по <see cref="PersonListFilter.Email"/>,
/// пробрасывание <see cref="CancellationToken"/>, а также обращение к
/// <c>unitOfWork.GetRepository&lt;PersonEntity&gt;()</c>.
/// </summary>
public sealed class PersonReadListQueryHandlerTests
{
    /// <summary>
    /// Создаёт тестовую сущность <see cref="PersonEntity"/> с уникальным идентификатором.
    /// </summary>
    /// <param name="id">Индекс для генерации имени и email.</param>
    /// <returns>Экземпляр <see cref="PersonEntity"/>.</returns>
    private static PersonEntity CreatePerson(int id) =>
        PersonEntity.Create($"Person-{id}", $"person-{id}@example.com");

    /// <summary>
    /// Регистрирует <paramref name="count"/> сущностей <see cref="PersonEntity"/> в репозитории
    /// и настраивает проекцию в <see cref="PersonListPayload"/>.
    /// </summary>
    /// <param name="repository">Репозиторий <see cref="PersonEntity"/>.</param>
    /// <param name="count">Количество сущностей.</param>
    private static void Seed(Shared.Testing.Doubles.Repository.FakeRepository<PersonEntity> repository, int count)
    {
        repository.PayloadMapper = p => new PersonListPayload
        {
            Id = p.Id,
            Name = p.Name,
            Email = p.Email,
        };
        for (var i = 1; i <= count; i++)
        {
            repository.AddDirect(CreatePerson(i));
        }
    }

    /// <summary>
    /// <see cref="PersonReadListQueryHandler"/> при пустом репозитории
    /// возвращает пустую полезную нагрузку.
    /// </summary>
    [Fact]
    public async Task Handle_WithEmptyRepo_ReturnsEmptyPayload()
    {
        // Arrange
        var unitOfWork = new Shared.Testing.Doubles.Repository.FakeUnitOfWork();
        Seed(unitOfWork.GetOrCreateRepository<PersonEntity>(), 0);
        var sut = new PersonReadListQueryHandler(NullLoggerFactory.Instance, unitOfWork);
        var query = new PersonListQuery(new PersonListRequest(DalPattern.UnitOfWork)
        {
            PageNumber = 1,
            PageSize = 10,
        });

        // Act
        var response = await sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        response.Payload.Should().BeEmpty();
    }

    /// <summary>
    /// <see cref="PersonReadListQueryHandler"/> возвращает все элементы
    /// при размере страницы, превышающем общее количество.
    /// </summary>
    [Fact]
    public async Task Handle_WithItems_ReturnsAllItems()
    {
        // Arrange
        var unitOfWork = new Shared.Testing.Doubles.Repository.FakeUnitOfWork();
        Seed(unitOfWork.GetOrCreateRepository<PersonEntity>(), 3);
        var sut = new PersonReadListQueryHandler(NullLoggerFactory.Instance, unitOfWork);
        var query = new PersonListQuery(new PersonListRequest(DalPattern.UnitOfWork)
        {
            PageNumber = 1,
            PageSize = 10,
        });

        // Act
        var response = await sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        response.Payload.Should().HaveCount(3);
    }

    /// <summary>
    /// <see cref="PersonReadListQueryHandler"/> применяет пагинацию и возвращает
    /// корректную страницу (25 элементов, размер 10, страница 2 — 10 элементов).
    /// </summary>
    [Fact]
    public async Task Handle_AppliesPagination_ReturnsCorrectPage()
    {
        // Arrange
        var unitOfWork = new Shared.Testing.Doubles.Repository.FakeUnitOfWork();
        Seed(unitOfWork.GetOrCreateRepository<PersonEntity>(), 25);
        var sut = new PersonReadListQueryHandler(NullLoggerFactory.Instance, unitOfWork);
        var query = new PersonListQuery(new PersonListRequest(DalPattern.UnitOfWork)
        {
            PageNumber = 2,
            PageSize = 10,
        });

        // Act
        var response = await sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        response.Payload.Should().HaveCount(10);
        response.PageNumber.Should().Be(2);
        response.TotalPages.Should().Be(3);
    }

    /// <summary>
    /// <see cref="PersonReadListQueryHandler"/> применяет пагинацию и возвращает
    /// остаток данных на последней странице (25 элементов, размер 10, страница 3 — 5 элементов).
    /// </summary>
    [Fact]
    public async Task Handle_AppliesPagination_Page3_Returns5Items()
    {
        // Arrange
        var unitOfWork = new Shared.Testing.Doubles.Repository.FakeUnitOfWork();
        Seed(unitOfWork.GetOrCreateRepository<PersonEntity>(), 25);
        var sut = new PersonReadListQueryHandler(NullLoggerFactory.Instance, unitOfWork);
        var query = new PersonListQuery(new PersonListRequest(DalPattern.UnitOfWork)
        {
            PageNumber = 3,
            PageSize = 10,
        });

        // Act
        var response = await sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        response.Payload.Should().HaveCount(5);
        response.PageNumber.Should().Be(3);
        response.TotalPages.Should().Be(3);
    }

    /// <summary>
    /// <see cref="PersonReadListQueryHandler"/> пробрасывает <see cref="CancellationToken"/>
    /// в репозиторий.
    /// </summary>
    [Fact]
    public async Task Handle_PassesCancellationTokenToRepo()
    {
        // Arrange
        var unitOfWork = new Shared.Testing.Doubles.Repository.FakeUnitOfWork();
        var repository = unitOfWork.GetOrCreateRepository<PersonEntity>();
        Seed(repository, 5);
        repository.ExceptionToThrowOnGet = new OperationCanceledException();
        var sut = new PersonReadListQueryHandler(NullLoggerFactory.Instance, unitOfWork);
        var query = new PersonListQuery(new PersonListRequest(DalPattern.UnitOfWork)
        {
            PageNumber = 1,
            PageSize = 10,
        });

        // Act
        var act = () => sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    /// <see cref="PersonReadListQueryHandler"/> при <c>Filter = null</c> не выбрасывает
    /// исключений.
    /// </summary>
    [Fact]
    public async Task Handle_WithNullFilter_NoException()
    {
        // Arrange
        var unitOfWork = new Shared.Testing.Doubles.Repository.FakeUnitOfWork();
        Seed(unitOfWork.GetOrCreateRepository<PersonEntity>(), 3);
        var sut = new PersonReadListQueryHandler(NullLoggerFactory.Instance, unitOfWork);
        var query = new PersonListQuery(new PersonListRequest(DalPattern.UnitOfWork)
        {
            PageNumber = 1,
            PageSize = 10,
            Filter = null,
        });

        // Act
        var act = () => sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// <see cref="PersonReadListQueryHandler"/> при заполненном <see cref="PersonListFilter.Email"/>
    /// применяет соответствующий фильтр и возвращает только подходящие сущности.
    /// </summary>
    [Fact]
    public async Task Handle_WithEmailFilter_AppliesFilter()
    {
        // Arrange
        var unitOfWork = new Shared.Testing.Doubles.Repository.FakeUnitOfWork();
        Seed(unitOfWork.GetOrCreateRepository<PersonEntity>(), 3);
        var sut = new PersonReadListQueryHandler(NullLoggerFactory.Instance, unitOfWork);
        var query = new PersonListQuery(new PersonListRequest(DalPattern.UnitOfWork)
        {
            PageNumber = 1,
            PageSize = 10,
            Filter = new PersonListFilter { Email = "person-2@example.com" },
        });

        // Act
        var response = await sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        response.Payload.Should().ContainSingle()
            .Which.Email.Should().Be("person-2@example.com");
    }

    /// <summary>
    /// <see cref="PersonReadListQueryHandler"/> сохраняет <see cref="PageableRequest.PageNumber"/>
    /// из запроса в ответе.
    /// </summary>
    [Fact]
    public async Task Handle_PreservesRequestPageNumber()
    {
        // Arrange
        var unitOfWork = new Shared.Testing.Doubles.Repository.FakeUnitOfWork();
        Seed(unitOfWork.GetOrCreateRepository<PersonEntity>(), 30);
        var sut = new PersonReadListQueryHandler(NullLoggerFactory.Instance, unitOfWork);
        var query = new PersonListQuery(new PersonListRequest(DalPattern.UnitOfWork)
        {
            PageNumber = 3,
            PageSize = 10,
        });

        // Act
        var response = await sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        response.PageNumber.Should().Be(3);
    }

    /// <summary>
    /// <see cref="PersonReadListQueryHandler"/> возвращает ответ
    /// с корректно заполненными полями пагинации.
    /// </summary>
    [Fact]
    public async Task Handle_ReturnsResponse_WithCorrectPageMetadata()
    {
        // Arrange
        var unitOfWork = new Shared.Testing.Doubles.Repository.FakeUnitOfWork();
        Seed(unitOfWork.GetOrCreateRepository<PersonEntity>(), 5);
        var sut = new PersonReadListQueryHandler(NullLoggerFactory.Instance, unitOfWork);
        var query = new PersonListQuery(new PersonListRequest(DalPattern.UnitOfWork)
        {
            PageNumber = 1,
            PageSize = 2,
        });

        // Act
        var response = await sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(200);
        response.PageNumber.Should().Be(1);
        response.TotalPages.Should().Be(3);
    }

    /// <summary>
    /// <see cref="PersonReadListQueryHandler"/> обращается к
    /// <c>unitOfWork.GetRepository&lt;PersonEntity&gt;()</c>.
    /// </summary>
    [Fact]
    public async Task Handle_CallsGetRepositoryOnUnitOfWork()
    {
        // Arrange
        var unitOfWork = new CountingUnitOfWork();
        Seed(unitOfWork.GetOrCreateRepository<PersonEntity>(), 3);
        var sut = new PersonReadListQueryHandler(NullLoggerFactory.Instance, unitOfWork);
        var query = new PersonListQuery(new PersonListRequest(DalPattern.UnitOfWork)
        {
            PageNumber = 1,
            PageSize = 10,
        });

        // Act
        await sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        unitOfWork.GetRepositoryCallCount.Should().BeGreaterThan(0);
    }
}
