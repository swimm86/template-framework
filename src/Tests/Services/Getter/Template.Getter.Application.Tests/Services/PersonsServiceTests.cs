// ----------------------------------------------------------------------------------------------
// <copyright file="PersonsServiceTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Template.Domain.Entities;
using Template.Getter.Application.Abstractions.Enums;
using Template.Getter.Application.Abstractions.Features.Person.List.Request;
using Template.Getter.Application.Abstractions.Features.Person.List.Response;
using Template.Getter.Application.Services;
using Template.Getter.Application.Tests.TestDoubles;

namespace Template.Getter.Application.Tests.Services;

/// <summary>
/// Тесты <see cref="PersonsService"/>.
/// Покрывают все три <see cref="DalPattern"/> (UnitOfWork, Repository, Specification),
/// проверяют пагинацию, сортировку, пробрасывание <see cref="CancellationToken"/>
/// и корректность выбора репозитория.
/// </summary>
public sealed class PersonsServiceTests
{
    /// <summary>
    /// Создаёт тестовую сущность <see cref="Person"/> с уникальным идентификатором.
    /// </summary>
    /// <param name="id">Индекс для генерации имени и email.</param>
    /// <returns>Экземпляр <see cref="Person"/>.</returns>
    private static Person CreatePerson(int id) =>
        Person.Create($"Person-{id}", $"person-{id}@example.com");

    /// <summary>
    /// Регистрирует <paramref name="count"/> сущностей <see cref="Person"/> в репозитории
    /// и настраивает проекцию в <see cref="PersonListPayload"/>.
    /// </summary>
    /// <param name="repository">Репозиторий <see cref="Person"/>.</param>
    /// <param name="count">Количество сущностей.</param>
    private static void Seed(Shared.Testing.Doubles.Repository.FakeRepository<Person> repository, int count)
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

    #region 5 items

    /// <summary>
    /// <see cref="PersonsService.GetPersonsAsync"/> с <see cref="DalPattern.UnitOfWork"/>
    /// и пятью элементами возвращает <c>200 OK</c> и одну страницу.
    /// </summary>
    [Fact]
    public async Task GetPersonsAsync_UnitOfWorkPattern_5Items_Returns200Ok()
    {
        // Arrange
        var unitOfWork = new Shared.Testing.Doubles.Repository.FakeUnitOfWork();
        var repository = unitOfWork.GetOrCreateRepository<Person>();
        Seed(repository, 5);
        var sut = new PersonsService(unitOfWork, repository);
        var request = new PersonListRequest(DalPattern.UnitOfWork)
        {
            PageNumber = 1,
            PageSize = 10,
        };

        // Act
        var response = await sut.GetPersonsAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(StatusCodes.Status200OK);
        response.PageNumber.Should().Be(1);
        response.TotalPages.Should().Be(1);
        response.Payload.Should().HaveCount(5);
    }

    /// <summary>
    /// <see cref="PersonsService.GetPersonsAsync"/> с <see cref="DalPattern.Repository"/>
    /// и пятью элементами возвращает <c>200 OK</c> и одну страницу.
    /// </summary>
    [Fact]
    public async Task GetPersonsAsync_RepositoryPattern_5Items_Returns200Ok()
    {
        // Arrange
        var unitOfWork = new Shared.Testing.Doubles.Repository.FakeUnitOfWork();
        var repository = unitOfWork.GetOrCreateRepository<Person>();
        Seed(repository, 5);
        var sut = new PersonsService(unitOfWork, repository);
        var request = new PersonListRequest(DalPattern.Repository)
        {
            PageNumber = 1,
            PageSize = 10,
        };

        // Act
        var response = await sut.GetPersonsAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(StatusCodes.Status200OK);
        response.PageNumber.Should().Be(1);
        response.TotalPages.Should().Be(1);
        response.Payload.Should().HaveCount(5);
    }

    /// <summary>
    /// <see cref="PersonsService.GetPersonsAsync"/> с <see cref="DalPattern.Specification"/>
    /// и пятью элементами возвращает <c>200 OK</c> и одну страницу.
    /// </summary>
    [Fact]
    public async Task GetPersonsAsync_SpecificationPattern_5Items_Returns200Ok()
    {
        // Arrange
        var unitOfWork = new Shared.Testing.Doubles.Repository.FakeUnitOfWork();
        var repository = unitOfWork.GetOrCreateRepository<Person>();
        Seed(repository, 5);
        var sut = new PersonsService(unitOfWork, repository);
        var request = new PersonListRequest(DalPattern.Specification)
        {
            PageNumber = 1,
            PageSize = 10,
        };

        // Act
        var response = await sut.GetPersonsAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(StatusCodes.Status200OK);
        response.PageNumber.Should().Be(1);
        response.TotalPages.Should().Be(1);
        response.Payload.Should().HaveCount(5);
    }

    #endregion

    #region Empty

    /// <summary>
    /// <see cref="PersonsService.GetPersonsAsync"/> с <see cref="DalPattern.UnitOfWork"/>
    /// и пустым репозиторием возвращает <c>204 No Content</c>.
    /// </summary>
    [Fact]
    public async Task GetPersonsAsync_UnitOfWorkPattern_EmptyRepo_Returns204NoContent()
    {
        // Arrange
        var unitOfWork = new Shared.Testing.Doubles.Repository.FakeUnitOfWork();
        var repository = unitOfWork.GetOrCreateRepository<Person>();
        Seed(repository, 0);
        var sut = new PersonsService(unitOfWork, repository);
        var request = new PersonListRequest(DalPattern.UnitOfWork)
        {
            PageNumber = 1,
            PageSize = 10,
        };

        // Act
        var response = await sut.GetPersonsAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(StatusCodes.Status204NoContent);
        response.Payload.Should().BeEmpty();
    }

    /// <summary>
    /// <see cref="PersonsService.GetPersonsAsync"/> с <see cref="DalPattern.Repository"/>
    /// и пустым репозиторием возвращает <c>204 No Content</c>.
    /// </summary>
    [Fact]
    public async Task GetPersonsAsync_RepositoryPattern_EmptyRepo_Returns204NoContent()
    {
        // Arrange
        var unitOfWork = new Shared.Testing.Doubles.Repository.FakeUnitOfWork();
        var repository = unitOfWork.GetOrCreateRepository<Person>();
        Seed(repository, 0);
        var sut = new PersonsService(unitOfWork, repository);
        var request = new PersonListRequest(DalPattern.Repository)
        {
            PageNumber = 1,
            PageSize = 10,
        };

        // Act
        var response = await sut.GetPersonsAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(StatusCodes.Status204NoContent);
        response.Payload.Should().BeEmpty();
    }

    /// <summary>
    /// <see cref="PersonsService.GetPersonsAsync"/> с <see cref="DalPattern.Specification"/>
    /// и пустым репозиторием возвращает <c>204 No Content</c>.
    /// </summary>
    [Fact]
    public async Task GetPersonsAsync_SpecificationPattern_EmptyRepo_Returns204NoContent()
    {
        // Arrange
        var unitOfWork = new Shared.Testing.Doubles.Repository.FakeUnitOfWork();
        var repository = unitOfWork.GetOrCreateRepository<Person>();
        Seed(repository, 0);
        var sut = new PersonsService(unitOfWork, repository);
        var request = new PersonListRequest(DalPattern.Specification)
        {
            PageNumber = 1,
            PageSize = 10,
        };

        // Act
        var response = await sut.GetPersonsAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(StatusCodes.Status204NoContent);
        response.Payload.Should().BeEmpty();
    }

    #endregion

    #region Pagination

    /// <summary>
    /// <see cref="PersonsService.GetPersonsAsync"/> с <see cref="DalPattern.UnitOfWork"/>,
    /// 25 элементами, размером страницы 10 и страницей 2 возвращает 10 элементов.
    /// </summary>
    [Fact]
    public async Task GetPersonsAsync_UnitOfWorkPattern_Page2Of3_Returns10Items()
    {
        // Arrange
        var unitOfWork = new Shared.Testing.Doubles.Repository.FakeUnitOfWork();
        var repository = unitOfWork.GetOrCreateRepository<Person>();
        Seed(repository, 25);
        var sut = new PersonsService(unitOfWork, repository);
        var request = new PersonListRequest(DalPattern.UnitOfWork)
        {
            PageNumber = 2,
            PageSize = 10,
        };

        // Act
        var response = await sut.GetPersonsAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.Payload.Should().HaveCount(10);
        response.PageNumber.Should().Be(2);
        response.TotalPages.Should().Be(3);
    }

    /// <summary>
    /// <see cref="PersonsService.GetPersonsAsync"/> с <see cref="DalPattern.Repository"/>,
    /// 25 элементами, размером страницы 10 и страницей 2 возвращает 10 элементов.
    /// </summary>
    [Fact]
    public async Task GetPersonsAsync_RepositoryPattern_Page2Of3_Returns10Items()
    {
        // Arrange
        var unitOfWork = new Shared.Testing.Doubles.Repository.FakeUnitOfWork();
        var repository = unitOfWork.GetOrCreateRepository<Person>();
        Seed(repository, 25);
        var sut = new PersonsService(unitOfWork, repository);
        var request = new PersonListRequest(DalPattern.Repository)
        {
            PageNumber = 2,
            PageSize = 10,
        };

        // Act
        var response = await sut.GetPersonsAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.Payload.Should().HaveCount(10);
        response.PageNumber.Should().Be(2);
        response.TotalPages.Should().Be(3);
    }

    /// <summary>
    /// <see cref="PersonsService.GetPersonsAsync"/> с <see cref="DalPattern.Specification"/>,
    /// 25 элементами, размером страницы 10 и страницей 2 возвращает 10 элементов.
    /// </summary>
    [Fact]
    public async Task GetPersonsAsync_SpecificationPattern_Page2Of3_Returns10Items()
    {
        // Arrange
        var unitOfWork = new Shared.Testing.Doubles.Repository.FakeUnitOfWork();
        var repository = unitOfWork.GetOrCreateRepository<Person>();
        Seed(repository, 25);
        var sut = new PersonsService(unitOfWork, repository);
        var request = new PersonListRequest(DalPattern.Specification)
        {
            PageNumber = 2,
            PageSize = 10,
        };

        // Act
        var response = await sut.GetPersonsAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.Payload.Should().HaveCount(10);
        response.PageNumber.Should().Be(2);
        response.TotalPages.Should().Be(3);
    }

    #endregion

    #region Sort

    /// <summary>
    /// <see cref="PersonsService.GetPersonsAsync"/> с <see cref="DalPattern.UnitOfWork"/>
    /// применяет сортировку по <c>Name</c> в порядке возрастания.
    /// </summary>
    [Fact]
    public async Task GetPersonsAsync_UnitOfWorkPattern_AppliesSortOptionsByName()
    {
        // Arrange
        var unitOfWork = new Shared.Testing.Doubles.Repository.FakeUnitOfWork();
        var repository = unitOfWork.GetOrCreateRepository<Person>();
        Seed(repository, 5);
        var sut = new PersonsService(unitOfWork, repository);
        var request = new PersonListRequest(DalPattern.UnitOfWork)
        {
            PageNumber = 1,
            PageSize = 10,
            SortOptions = ["Name.asc"],
        };

        // Act
        var response = await sut.GetPersonsAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.Payload.Should().BeInAscendingOrder(p => p.Name);
    }

    /// <summary>
    /// <see cref="PersonsService.GetPersonsAsync"/> с <see cref="DalPattern.Repository"/>
    /// применяет сортировку по <c>Name</c> в порядке возрастания.
    /// </summary>
    [Fact]
    public async Task GetPersonsAsync_RepositoryPattern_AppliesSortOptionsByName()
    {
        // Arrange
        var unitOfWork = new Shared.Testing.Doubles.Repository.FakeUnitOfWork();
        var repository = unitOfWork.GetOrCreateRepository<Person>();
        Seed(repository, 5);
        var sut = new PersonsService(unitOfWork, repository);
        var request = new PersonListRequest(DalPattern.Repository)
        {
            PageNumber = 1,
            PageSize = 10,
            SortOptions = ["Name.asc"],
        };

        // Act
        var response = await sut.GetPersonsAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.Payload.Should().BeInAscendingOrder(p => p.Name);
    }

    /// <summary>
    /// <see cref="PersonsService.GetPersonsAsync"/> с <see cref="DalPattern.Specification"/>
    /// применяет сортировку по <c>Name</c> в порядке возрастания.
    /// </summary>
    [Fact]
    public async Task GetPersonsAsync_SpecificationPattern_AppliesSortOptionsByName()
    {
        // Arrange
        var unitOfWork = new Shared.Testing.Doubles.Repository.FakeUnitOfWork();
        var repository = unitOfWork.GetOrCreateRepository<Person>();
        Seed(repository, 5);
        var sut = new PersonsService(unitOfWork, repository);
        var request = new PersonListRequest(DalPattern.Specification)
        {
            PageNumber = 1,
            PageSize = 10,
            SortOptions = ["Name.asc"],
        };

        // Act
        var response = await sut.GetPersonsAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.Payload.Should().BeInAscendingOrder(p => p.Name);
    }

    #endregion

    #region Page number preservation

    /// <summary>
    /// <see cref="PersonsService.GetPersonsAsync"/> с <see cref="DalPattern.UnitOfWork"/>
    /// сохраняет <see cref="PersonListRequest.PageNumber"/> из запроса.
    /// </summary>
    [Fact]
    public async Task GetPersonsAsync_UnitOfWorkPattern_PreservesRequestPageNumber()
    {
        // Arrange
        var unitOfWork = new Shared.Testing.Doubles.Repository.FakeUnitOfWork();
        var repository = unitOfWork.GetOrCreateRepository<Person>();
        Seed(repository, 30);
        var sut = new PersonsService(unitOfWork, repository);
        var request = new PersonListRequest(DalPattern.UnitOfWork)
        {
            PageNumber = 3,
            PageSize = 10,
        };

        // Act
        var response = await sut.GetPersonsAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.PageNumber.Should().Be(3);
    }

    /// <summary>
    /// <see cref="PersonsService.GetPersonsAsync"/> с <see cref="DalPattern.Repository"/>
    /// сохраняет <see cref="PersonListRequest.PageNumber"/> из запроса.
    /// </summary>
    [Fact]
    public async Task GetPersonsAsync_RepositoryPattern_PreservesRequestPageNumber()
    {
        // Arrange
        var unitOfWork = new Shared.Testing.Doubles.Repository.FakeUnitOfWork();
        var repository = unitOfWork.GetOrCreateRepository<Person>();
        Seed(repository, 30);
        var sut = new PersonsService(unitOfWork, repository);
        var request = new PersonListRequest(DalPattern.Repository)
        {
            PageNumber = 3,
            PageSize = 10,
        };

        // Act
        var response = await sut.GetPersonsAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.PageNumber.Should().Be(3);
    }

    /// <summary>
    /// <see cref="PersonsService.GetPersonsAsync"/> с <see cref="DalPattern.Specification"/>
    /// сохраняет <see cref="PersonListRequest.PageNumber"/> из запроса.
    /// </summary>
    [Fact]
    public async Task GetPersonsAsync_SpecificationPattern_PreservesRequestPageNumber()
    {
        // Arrange
        var unitOfWork = new Shared.Testing.Doubles.Repository.FakeUnitOfWork();
        var repository = unitOfWork.GetOrCreateRepository<Person>();
        Seed(repository, 30);
        var sut = new PersonsService(unitOfWork, repository);
        var request = new PersonListRequest(DalPattern.Specification)
        {
            PageNumber = 3,
            PageSize = 10,
        };

        // Act
        var response = await sut.GetPersonsAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.PageNumber.Should().Be(3);
    }

    #endregion

    #region Cancellation token

    /// <summary>
    /// <see cref="PersonsService.GetPersonsAsync"/> с <see cref="DalPattern.UnitOfWork"/>
    /// пробрасывает <see cref="CancellationToken"/> в репозиторий.
    /// </summary>
    [Fact]
    public async Task GetPersonsAsync_UnitOfWorkPattern_PassesCancellationTokenToRepo()
    {
        // Arrange
        var unitOfWork = new Shared.Testing.Doubles.Repository.FakeUnitOfWork();
        var repository = unitOfWork.GetOrCreateRepository<Person>();
        Seed(repository, 5);
        repository.ExceptionToThrowOnGet = new OperationCanceledException();
        var sut = new PersonsService(unitOfWork, repository);
        var request = new PersonListRequest(DalPattern.UnitOfWork)
        {
            PageNumber = 1,
            PageSize = 10,
        };

        // Act
        var act = () => sut.GetPersonsAsync(request, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    /// <see cref="PersonsService.GetPersonsAsync"/> с <see cref="DalPattern.Repository"/>
    /// пробрасывает <see cref="CancellationToken"/> в репозиторий.
    /// </summary>
    [Fact]
    public async Task GetPersonsAsync_RepositoryPattern_PassesCancellationTokenToRepo()
    {
        // Arrange
        var unitOfWork = new Shared.Testing.Doubles.Repository.FakeUnitOfWork();
        var repository = unitOfWork.GetOrCreateRepository<Person>();
        Seed(repository, 5);
        repository.ExceptionToThrowOnGet = new OperationCanceledException();
        var sut = new PersonsService(unitOfWork, repository);
        var request = new PersonListRequest(DalPattern.Repository)
        {
            PageNumber = 1,
            PageSize = 10,
        };

        // Act
        var act = () => sut.GetPersonsAsync(request, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    /// <see cref="PersonsService.GetPersonsAsync"/> с <see cref="DalPattern.Specification"/>
    /// пробрасывает <see cref="CancellationToken"/> в репозиторий.
    /// </summary>
    [Fact]
    public async Task GetPersonsAsync_SpecificationPattern_PassesCancellationTokenToRepo()
    {
        // Arrange
        var unitOfWork = new Shared.Testing.Doubles.Repository.FakeUnitOfWork();
        var repository = unitOfWork.GetOrCreateRepository<Person>();
        Seed(repository, 5);
        repository.ExceptionToThrowOnGet = new OperationCanceledException();
        var sut = new PersonsService(unitOfWork, repository);
        var request = new PersonListRequest(DalPattern.Specification)
        {
            PageNumber = 1,
            PageSize = 10,
        };

        // Act
        var act = () => sut.GetPersonsAsync(request, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Repository injection

    /// <summary>
    /// <see cref="PersonsService.GetPersonsAsync"/> с <see cref="DalPattern.Repository"/>
    /// использует внедрённый <see cref="Shared.Domain.Core.Dal.Repository.Interfaces.IRepository{T}"/>,
    /// не обращаясь к <c>unitOfWork.GetRepository&lt;Person&gt;()</c>.
    /// </summary>
    [Fact]
    public async Task GetPersonsAsync_RepositoryPattern_UsesInjectedRepository()
    {
        // Arrange
        var unitOfWork = new CountingUnitOfWork();
        var repository = new Shared.Testing.Doubles.Repository.FakeRepository<Person>();
        Seed(repository, 5);
        var sut = new PersonsService(unitOfWork, repository);
        var request = new PersonListRequest(DalPattern.Repository)
        {
            PageNumber = 1,
            PageSize = 10,
        };

        // Act
        await sut.GetPersonsAsync(request, TestContext.Current.CancellationToken);

        // Assert
        unitOfWork.GetRepositoryCallCount.Should().Be(0);
    }

    #endregion

    #region Unsupported DalPattern

    /// <summary>
    /// <see cref="PersonsService.GetPersonsAsync"/> с неподдерживаемым значением
    /// <see cref="DalPattern"/> выбрасывает <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    [Fact]
    public async Task GetPersonsAsync_WithUnsupportedDalPattern_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var unitOfWork = new Shared.Testing.Doubles.Repository.FakeUnitOfWork();
        var repository = unitOfWork.GetOrCreateRepository<Person>();
        Seed(repository, 5);
        var sut = new PersonsService(unitOfWork, repository);
        var request = new PersonListRequest((DalPattern)999)
        {
            PageNumber = 1,
            PageSize = 10,
        };

        // Act
        var act = () => sut.GetPersonsAsync(request, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    #endregion
}
