// ----------------------------------------------------------------------------------------------
// <copyright file="GetterClientTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Text.Json;
using Shared.Application.Core.ApiClient.Validators.Interfaces;
using Shared.Domain.Core.Converters.Interfaces;
using Shared.Domain.Core.Utils.Interfaces;
using Shared.Testing.Http;
using Template.Bff.Application.HttpClients;
using Template.Bff.Application.HttpClients.Enums;
using Template.Bff.Application.Features.Queries.Person.Cqrs.List.Requests;
using Template.Getter.Application.Abstractions.Enums;
using Template.Getter.Application.Abstractions.Features.Person.List.Response;

namespace Template.Bff.Application.Tests.HttpClients;

/// <summary>
/// Тесты <see cref="GetterClient"/>.
/// Проверяют корректность маршрутизации по <see cref="GetPersonsPattern"/>,
/// сериализацию тела запроса, пробрасывание <see cref="CancellationToken"/>
/// и HTTP-метод.
/// </summary>
public sealed class GetterClientTests
{
    private const string BaseAddress = "http://localhost/";

    /// <summary>
    /// <c>GetPersonsAsync</c> с <see cref="GetPersonsPattern.Cqrs"/>
    /// вызывает относительный путь <c>persons/cqrs/list</c>.
    /// </summary>
    [Fact]
    public async Task GetPersonsAsync_WithCqrsPattern_CallsCqrsEndpoint()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        handler.QueueJsonResponse(new PersonListResponse());
        var sut = CreateSut(handler);
        var request = new PersonListRequest(DalPattern.Repository, UseCqrs: true);

        // Act
        await sut.GetPersonsAsync(request, GetPersonsPattern.Cqrs, TestContext.Current.CancellationToken);

        // Assert
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.AbsolutePath.Should().Contain("persons/cqrs/list");
    }

    /// <summary>
    /// <c>GetPersonsAsync</c> с <see cref="GetPersonsPattern.Services"/>
    /// вызывает относительный путь <c>persons/services/list</c>.
    /// </summary>
    [Fact]
    public async Task GetPersonsAsync_WithServicesPattern_CallsServicesEndpoint()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        handler.QueueJsonResponse(new PersonListResponse());
        var sut = CreateSut(handler);
        var request = new PersonListRequest(DalPattern.Repository, UseCqrs: false);

        // Act
        await sut.GetPersonsAsync(request, GetPersonsPattern.Services, TestContext.Current.CancellationToken);

        // Assert
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.AbsolutePath.Should().Contain("persons/services/list");
    }

    /// <summary>
    /// <c>GetPersonsAsync</c> десериализует JSON-ответ
    /// в <see cref="PersonListResponse"/>.
    /// </summary>
    [Fact]
    public async Task GetPersonsAsync_DeserializesResponseToPersonListResponse()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        handler.QueueJsonResponse(new PersonListResponse
        {
            PageNumber = 7,
            TotalPages = 42,
        });
        var sut = CreateSut(handler);
        var request = new PersonListRequest(DalPattern.UnitOfWork, UseCqrs: true);

        // Act
        var response = await sut.GetPersonsAsync(request, GetPersonsPattern.Cqrs, TestContext.Current.CancellationToken);

        // Assert
        response.Should().NotBeNull();
        response!.PageNumber.Should().Be(7);
        response.TotalPages.Should().Be(42);
    }

    /// <summary>
    /// <c>GetPersonsAsync</c> сериализует <see cref="PersonListRequest"/>
    /// в JSON-тело запроса.
    /// </summary>
    [Fact]
    public async Task GetPersonsAsync_PassesRequestAsJsonBody()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        handler.QueueJsonResponse(new PersonListResponse());
        var sut = CreateSut(handler);
        var request = new PersonListRequest(DalPattern.Specification, UseCqrs: true);

        // Act
        await sut.GetPersonsAsync(request, GetPersonsPattern.Cqrs, TestContext.Current.CancellationToken);

        // Assert
        handler.LastRequestBody.Should().NotBeNullOrWhiteSpace();
        using var document = JsonDocument.Parse(handler.LastRequestBody!);
        document.RootElement.GetProperty("useCqrs").GetBoolean().Should().BeTrue();
    }

    /// <summary>
    /// <c>GetPersonsAsync</c> пробрасывает <see cref="CancellationToken"/>
    /// в базовый <see cref="HttpMessageHandler"/>.
    /// </summary>
    [Fact]
    public async Task GetPersonsAsync_PassesCancellationToken()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        var recording = new CancellationTokenRecordingHandler(handler);
        var factory = new FakeHttpClientFactory(recording);
        var sut = new GetterClient(
            factory,
            new AcceptingUriValidator(),
            new AcceptingResponseValidator(),
            new NoOpPropertyGetter());
        handler.QueueJsonResponse(new PersonListResponse());

        var cts = new CancellationTokenSource();
        var token = cts.Token;
        var request = new PersonListRequest(DalPattern.Repository, UseCqrs: true);

        // Act
        await sut.GetPersonsAsync(request, GetPersonsPattern.Cqrs, token);

        // Assert
        recording.CapturedTokens.Should().HaveCount(1);
        recording.CapturedTokens[0].CanBeCanceled.Should().BeTrue();
        cts.Dispose();
    }

    /// <summary>
    /// <c>GetPersonsAsync</c> с неподдерживаемым значением
    /// <see cref="GetPersonsPattern"/> выбрасывает <see cref="ArgumentException"/>.
    /// </summary>
    [Fact]
    public async Task GetPersonsAsync_WithUnknownPattern_ThrowsArgumentException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        handler.QueueJsonResponse(new PersonListResponse());
        var sut = CreateSut(handler);
        var request = new PersonListRequest(DalPattern.Repository, UseCqrs: true);
        var unknown = (GetPersonsPattern)999;

        // Act
        var act = () => sut.GetPersonsAsync(request, unknown, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>
    /// <c>GetPersonsAsync</c> отправляет HTTP-запрос методом POST.
    /// </summary>
    [Fact]
    public async Task GetPersonsAsync_VerifiesRequestMethod_IsPost()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        handler.QueueJsonResponse(new PersonListResponse());
        var sut = CreateSut(handler);
        var request = new PersonListRequest(DalPattern.Repository, UseCqrs: true);

        // Act
        await sut.GetPersonsAsync(request, GetPersonsPattern.Cqrs, TestContext.Current.CancellationToken);

        // Assert
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
    }

    /// <summary>
    /// <c>GetPersonsAsync</c> отправляет ровно один HTTP-запрос.
    /// </summary>
    [Fact]
    public async Task GetPersonsAsync_SendsExactlyOneRequest()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        handler.QueueJsonResponse(new PersonListResponse());
        var sut = CreateSut(handler);
        var request = new PersonListRequest(DalPattern.Repository, UseCqrs: true);

        // Act
        await sut.GetPersonsAsync(request, GetPersonsPattern.Cqrs, TestContext.Current.CancellationToken);

        // Assert
        handler.RequestCount.Should().Be(1);
    }

    private static GetterClient CreateSut(FakeHttpMessageHandler handler)
    {
        return new GetterClient(
            new FakeHttpClientFactory(handler),
            new AcceptingUriValidator(),
            new AcceptingResponseValidator(),
            new NoOpPropertyGetter());
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public FakeHttpClientFactory(HttpMessageHandler handler)
        {
            _handler = handler;
        }

        public HttpClient CreateClient(string name)
        {
            return new HttpClient(_handler, disposeHandler: false)
            {
                BaseAddress = new Uri(BaseAddress),
            };
        }
    }

    private sealed class AcceptingUriValidator : IUriValidator
    {
        public void Validate(string uri)
        {
        }
    }

    private sealed class AcceptingResponseValidator : IResponseValidator
    {
        public Task ValidateAsync(
            HttpResponseMessage httpResponse,
            string clientName,
            string? logUri = null,
            object? logContent = null,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class NoOpPropertyGetter : IPropertyGetter
    {
        public object? GetProperty(object? obj, string propertyName, bool throwIfNotFound = true)
        {
            return null;
        }

        public string GetPropertyAsString(object obj, string propertyName, IObjectToStringConverter? converter = null)
        {
            return string.Empty;
        }
    }

    private sealed class CancellationTokenRecordingHandler : DelegatingHandler
    {
        public List<CancellationToken> CapturedTokens { get; } = new();

        public CancellationTokenRecordingHandler(HttpMessageHandler inner)
            : base(inner)
        {
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CapturedTokens.Add(cancellationToken);
            return base.SendAsync(request, cancellationToken);
        }
    }
}
