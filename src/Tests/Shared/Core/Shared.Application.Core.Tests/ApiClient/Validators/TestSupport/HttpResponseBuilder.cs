// ----------------------------------------------------------------------------------------------
// <copyright file="HttpResponseBuilder.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Net;
using System.Text;

namespace Shared.Application.Core.Tests.ApiClient.Validators.TestSupport;

/// <summary>
/// Тестовый построитель <see cref="HttpResponseMessage"/>.
/// </summary>
internal sealed class HttpResponseBuilder
{
    private HttpStatusCode _statusCode = HttpStatusCode.OK;
    private string? _body;
    private string? _contentType;
    private string? _requestUri;
    private bool _nullContent;
    private Exception? _throwOnSerialize;
    private bool _cancelOnSerialize;

    public static HttpResponseBuilder WithStatusCode(HttpStatusCode statusCode) => new() { _statusCode = statusCode };

    public HttpResponseBuilder WithJsonBody(string body)
    {
        _body = body;
        _contentType = "application/problem+json";
        return this;
    }

    public HttpResponseBuilder WithEmptyBody()
    {
        _body = string.Empty;
        _contentType = "text/plain";
        return this;
    }

    public HttpResponseBuilder WithNullContent()
    {
        _nullContent = true;
        return this;
    }

    public HttpResponseBuilder WithRequestUri(string uri)
    {
        _requestUri = uri;
        return this;
    }

    public HttpResponseBuilder WithoutRequestMessage()
    {
        _requestUri = null;
        return this;
    }

    public HttpResponseBuilder WithContentType(string contentType)
    {
        _contentType = contentType;
        return this;
    }

    public HttpResponseBuilder WithThrowingContent(Exception exception)
    {
        _throwOnSerialize = exception;
        _cancelOnSerialize = false;
        _nullContent = false;
        return this;
    }

    public HttpResponseBuilder WithCancellingContent()
    {
        _cancelOnSerialize = true;
        _throwOnSerialize = null;
        _nullContent = false;
        return this;
    }

    public HttpResponseMessage Build()
    {
        var response = new HttpResponseMessage(_statusCode);
        if (_throwOnSerialize != null)
        {
            response.Content = new ThrowingHttpContent(_throwOnSerialize);
        }
        else if (_cancelOnSerialize)
        {
            response.Content = new CancellingHttpContent();
        }
        else if (!_nullContent && _body != null)
        {
            response.Content = new StringContent(_body, Encoding.UTF8, _contentType ?? "application/json");
        }
        if (_requestUri != null)
        {
            response.RequestMessage = new HttpRequestMessage(HttpMethod.Get, _requestUri);
        }
        return response;
    }

    private sealed class ThrowingHttpContent : HttpContent
    {
        private readonly Exception _exception;

        public ThrowingHttpContent(Exception exception) { _exception = exception; }

        protected override Task SerializeToStreamAsync(Stream stream, System.Net.TransportContext context) => Task.FromException(_exception);

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }
    }

    private sealed class CancellingHttpContent : HttpContent
    {
        protected override async Task SerializeToStreamAsync(Stream stream, System.Net.TransportContext context)
        {
            await Task.Yield();
            throw new OperationCanceledException("Cancelled during content read");
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }
    }
}
