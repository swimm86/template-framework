// ----------------------------------------------------------------------------------------------
// <copyright file="PersonListQueryHandlerTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Template.Bff.Application.Features.Queries.Person.Cqrs.List;
using Template.Bff.Application.HttpClients.Enums;
using Template.Bff.Application.Interfaces.HttpClients;
using Template.Getter.Application.Abstractions.Enums;
using Template.Getter.Application.Abstractions.Features.Person.List.Response;
using PersonListRequest = Template.Bff.Application.Features.Queries.Person.Cqrs.List.Requests.PersonListRequest;

namespace Template.Bff.Application.Tests.Features.Queries.Person.Cqrs.List;

/// <summary>
/// Тесты <see cref="PersonListQueryHandler"/>.
/// Проверяют выбор паттерна <see cref="GetPersonsPattern"/>
/// по флагу <c>UseCqrs</c>, пробрасывание запроса и <see cref="CancellationToken"/>,
/// а также возврат ответа от API-клиента.
/// </summary>
public sealed class PersonListQueryHandlerTests
{
    /// <summary>
    /// При <c>UseCqrs = true</c> обработчик вызывает
    /// <see cref="IGetterClient.GetPersonsAsync"/> с <see cref="GetPersonsPattern.Cqrs"/>.
    /// </summary>
    [Fact]
    public async Task Handle_WithUseCqrsTrue_CallsGetterWithCqrsPattern()
    {
        // Arrange
        var fakeGetter = new FakeGetterClient(new PersonListResponse());
        var sut = new PersonListQueryHandler(fakeGetter);
        var query = new PersonListQuery(
            new PersonListRequest(DalPattern.Repository, UseCqrs: true));

        // Act
        await sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        fakeGetter.LastPattern.Should().Be(GetPersonsPattern.Cqrs);
    }

    /// <summary>
    /// При <c>UseCqrs = false</c> обработчик вызывает
    /// <see cref="IGetterClient.GetPersonsAsync"/> с <see cref="GetPersonsPattern.Services"/>.
    /// </summary>
    [Fact]
    public async Task Handle_WithUseCqrsFalse_CallsGetterWithServicesPattern()
    {
        // Arrange
        var fakeGetter = new FakeGetterClient(new PersonListResponse());
        var sut = new PersonListQueryHandler(fakeGetter);
        var query = new PersonListQuery(
            new PersonListRequest(DalPattern.Repository, UseCqrs: false));

        // Act
        await sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        fakeGetter.LastPattern.Should().Be(GetPersonsPattern.Services);
    }

    /// <summary>
    /// Обработчик пробрасывает <see cref="PersonListRequest"/>
    /// из <see cref="PersonListQuery"/> в <see cref="IGetterClient"/>.
    /// </summary>
    [Fact]
    public async Task Handle_PassesRequestToGetter()
    {
        // Arrange
        var fakeGetter = new FakeGetterClient(new PersonListResponse());
        var sut = new PersonListQueryHandler(fakeGetter);
        var request = new PersonListRequest(DalPattern.Specification, UseCqrs: true);
        var query = new PersonListQuery(request);

        // Act
        await sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        fakeGetter.LastRequest.Should().BeSameAs(request);
    }

    /// <summary>
    /// Обработчик пробрасывает <see cref="CancellationToken"/>
    /// в <see cref="IGetterClient.GetPersonsAsync"/>.
    /// </summary>
    [Fact]
    public async Task Handle_PassesCancellationToken()
    {
        // Arrange
        var fakeGetter = new FakeGetterClient(new PersonListResponse());
        var sut = new PersonListQueryHandler(fakeGetter);
        var query = new PersonListQuery(
            new PersonListRequest(DalPattern.Repository, UseCqrs: true));

        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await sut.Handle(query, token);

        // Assert
        fakeGetter.LastCancellationToken.CanBeCanceled.Should().BeTrue();
        cts.Dispose();
    }

    /// <summary>
    /// Обработчик возвращает ответ, полученный от <see cref="IGetterClient"/>,
    /// без модификаций.
    /// </summary>
    [Fact]
    public async Task Handle_ReturnsGetterResponse()
    {
        // Arrange
        var expected = new PersonListResponse
        {
            PageNumber = 3,
            TotalPages = 9,
        };
        var fakeGetter = new FakeGetterClient(expected);
        var sut = new PersonListQueryHandler(fakeGetter);
        var query = new PersonListQuery(
            new PersonListRequest(DalPattern.Repository, UseCqrs: true));

        // Act
        var actual = await sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        actual.Should().BeSameAs(expected);
    }

    private sealed class FakeGetterClient : IGetterClient
    {
        private readonly PersonListResponse _response;

        public FakeGetterClient(PersonListResponse response)
        {
            _response = response;
        }

        public PersonListRequest? LastRequest { get; private set; }

        public GetPersonsPattern? LastPattern { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public Task<PersonListResponse> GetPersonsAsync(
            Template.Getter.Application.Abstractions.Features.Person.List.Request.PersonListRequest request,
            GetPersonsPattern pattern,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request as PersonListRequest;
            LastPattern = pattern;
            LastCancellationToken = cancellationToken;
            return Task.FromResult(_response);
        }
    }
}
