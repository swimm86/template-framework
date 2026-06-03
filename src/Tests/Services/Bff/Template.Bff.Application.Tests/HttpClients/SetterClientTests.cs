// ----------------------------------------------------------------------------------------------
// <copyright file="SetterClientTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.ApiClient.Validators.Interfaces;
using Shared.Domain.Core.Converters.Interfaces;
using Shared.Domain.Core.Utils.Interfaces;
using Shared.Testing.Http;
using Template.Bff.Application.HttpClients;
using Template.Setter.Application.Abstractions.Features.Person.Create.Request;
using Template.Setter.Application.Abstractions.Features.Person.Create.Response;

namespace Template.Bff.Application.Tests.HttpClients;

/// <summary>
/// Тесты <see cref="SetterClient"/>.
/// Проверяют корректность относительного пути, сериализацию тела запроса,
/// пробрасывание <see cref="CancellationToken"/> и HTTP-метод.
/// </summary>
public sealed class SetterClientTests
{
    private const string BaseAddress = "http://localhost/";

    /// <summary>
    /// <c>CreatePersonAsync</c> вызывает относительный путь <c>persons/create</c>.
    /// </summary>
    [Fact]
    public async Task CreatePersonAsync_CallsCreateEndpoint()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        handler.QueueJsonResponse(new PersonCreateResponse());
        var sut = CreateSut(handler);
        var request = new PersonCreateRequest { Name = "John", Email = "john@example.com" };

        // Act
        await sut.CreatePersonAsync(request, TestContext.Current.CancellationToken);

        // Assert
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.AbsolutePath.Should().Contain("persons/create");
    }

    /// <summary>
    /// <c>CreatePersonAsync</c> десериализует JSON-ответ
    /// в <see cref="PersonCreateResponse"/>.
    /// </summary>
    [Fact]
    public async Task CreatePersonAsync_DeserializesResponseToPersonCreateResponse()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        handler.QueueJsonResponse(new PersonCreateResponse { Id = Guid.NewGuid() });
        var sut = CreateSut(handler);
        var request = new PersonCreateRequest { Name = "John", Email = "john@example.com" };

        // Act
        var response = await sut.CreatePersonAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.Should().NotBeNull();
        response!.Id.Should().NotBeNull();
    }

    /// <summary>
    /// <c>CreatePersonAsync</c> сериализует <see cref="PersonCreateRequest"/>
    /// в JSON-тело запроса.
    /// </summary>
    [Fact]
    public async Task CreatePersonAsync_PassesRequest()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        handler.QueueJsonResponse(new PersonCreateResponse());
        var sut = CreateSut(handler);
        var request = new PersonCreateRequest { Name = "Alice", Email = "alice@example.com" };

        // Act
        await sut.CreatePersonAsync(request, TestContext.Current.CancellationToken);

        // Assert
        handler.LastRequestBody.Should().NotBeNullOrWhiteSpace();
        handler.LastRequestBody.Should().Contain("Alice");
    }

    /// <summary>
    /// <c>CreatePersonAsync</c> пробрасывает <see cref="CancellationToken"/>
    /// в базовый <see cref="HttpMessageHandler"/>.
    /// </summary>
    [Fact]
    public async Task CreatePersonAsync_PassesCancellationToken()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        var recording = new CancellationTokenRecordingHandler(handler);
        var factory = new FakeHttpClientFactory(recording);
        var sut = new SetterClient(
            factory,
            new AcceptingUriValidator(),
            new AcceptingResponseValidator(),
            new NoOpPropertyGetter());
        handler.QueueJsonResponse(new PersonCreateResponse());

        var cts = new CancellationTokenSource();
        var token = cts.Token;
        var request = new PersonCreateRequest { Name = "John", Email = "john@example.com" };

        // Act
        await sut.CreatePersonAsync(request, token);

        // Assert
        recording.CapturedTokens.Should().HaveCount(1);
        recording.CapturedTokens[0].CanBeCanceled.Should().BeTrue();
        cts.Dispose();
    }

    /// <summary>
    /// <c>CreatePersonAsync</c> отправляет HTTP-запрос методом POST.
    /// </summary>
    [Fact]
    public async Task CreatePersonAsync_VerifiesRequestMethod_IsPost()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        handler.QueueJsonResponse(new PersonCreateResponse());
        var sut = CreateSut(handler);
        var request = new PersonCreateRequest { Name = "John", Email = "john@example.com" };

        // Act
        await sut.CreatePersonAsync(request, TestContext.Current.CancellationToken);

        // Assert
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
    }

    /// <summary>
    /// <c>CreatePersonAsync</c> отправляет ровно один HTTP-запрос.
    /// </summary>
    [Fact]
    public async Task CreatePersonAsync_SendsExactlyOneRequest()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        handler.QueueJsonResponse(new PersonCreateResponse());
        var sut = CreateSut(handler);
        var request = new PersonCreateRequest { Name = "John", Email = "john@example.com" };

        // Act
        await sut.CreatePersonAsync(request, TestContext.Current.CancellationToken);

        // Assert
        handler.RequestCount.Should().Be(1);
    }

    private static SetterClient CreateSut(FakeHttpMessageHandler handler)
    {
        return new SetterClient(
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
