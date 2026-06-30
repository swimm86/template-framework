// ----------------------------------------------------------------------------------------------
// <copyright file="ProxiedResponseValidator.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.ApiClient.Validators.Interfaces;
using Shared.Application.Core.ApiClient.Validators.Settings;
using Shared.Application.Core.Configuration.Extensions;
using Shared.Application.Core.Dto.Responses;
using Shared.Application.Core.Exceptions.Models;
using Shared.Common.Extensions;
using Shared.Common.Helpers;
using Shared.Domain.Core.Interfaces;

namespace Shared.Application.Core.ApiClient.Validators;

/// <summary>
/// Валидатор ответов, преобразующий HTTP-ошибки в <see cref="ProxiedException"/>.
/// </summary>
/// <remarks>
/// <para>
/// При ошибке читает тело ответа как <see cref="ProblemDetails"/> (или создаёт заглушку,
/// если JSON невалиден), извлекает <c>additionalData</c> и выбрасывает <see cref="ProxiedException"/>.
/// </para>
/// <para>
/// Если в массиве <c>errors</c> найден элемент со статусом 500, заголовок и описание
/// модифицируются для ясности.
/// </para>
/// <para>
/// При сбое построения <see cref="ProblemDetails"/> (например, ошибка чтения тела ответа)
/// выбрасывается <see cref="ProxiedException"/> с деградированными данными и корневой причиной
/// в <see cref="Exception.InnerException"/>. Корневая причина определяется как
/// <c>ex.InnerException ?? ex</c>, что исключает обёртки <see cref="System.Net.Http.HttpRequestException"/>
/// от фреймворка и сохраняет исходный <see cref="System.IO.IOException"/> для цепочки
/// <see cref="Exception.InnerException"/>, по которой проходит классификация транзиентных сбоев.
/// </para>
/// <para>
/// Длина логируемого тела ответа ограничена настройкой
/// <see cref="ProxiedResponseValidatorSettings.MaxLoggedBodyLength"/>.
/// </para>
/// </remarks>
/// <param name="logger">Экземпляр <see cref="ILogger"/> для работы с логированием.</param>
/// <param name="configuration">
/// <see cref="IConfiguration"/> для чтения <see cref="ProxiedResponseValidatorSettings"/>.
/// </param>
public sealed class ProxiedResponseValidator(
    ILogger<ProxiedResponseValidator> logger,
    IConfiguration configuration)
    : IResponseValidator
{
    private const string UnknownPathPlaceholder = "unknown";
    private const string DefaultProblemTitle = "Ошибка во время взаимодействия с внешним сервисом.";

    private static readonly string ErrorsPropertyName = nameof(ErrorResponse.Errors).ToCamelCase();
    private static readonly string DetailPropertyName = nameof(ProblemDetails.Detail).ToCamelCase();
    private static readonly string StatusPropertyName = nameof(ProblemDetails.Status).ToCamelCase();
    private static readonly string AdditionalDataPropertyName = nameof(IWithAdditionalData.AdditionalData).ToCamelCase();

    private static readonly JsonSerializerOptions LogJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ProxiedResponseValidatorSettings _settings =
        configuration.GetOptions<ProxiedResponseValidatorSettings>() ?? new();

    /// <inheritdoc />
    public async Task ValidateAsync(
        HttpResponseMessage httpResponse,
        string clientName,
        string? logUri = null,
        object? logContent = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpResponse);

        if (httpResponse.IsSuccessStatusCode)
        {
            return;
        }

        var absolutePath = ResolveAbsolutePath(httpResponse, logUri);
        var statusCode = (int)httpResponse.StatusCode;

        ProblemDetails? problemDetails = null;
        Exception? innerException = null;
        OperationCanceledException? canceledException = null;
        IReadOnlyDictionary<string, object>? additionalData = null;

        var requestInfo =
            $"\tClientName={clientName}{Environment.NewLine}" +
            $"\tPath={absolutePath}{Environment.NewLine}" +
            $"\tStatusCode={statusCode}{Environment.NewLine}" +
            $"\tRequestBody={SerializeForLog(logContent)}";

        try
        {
            var responseMessage = await ReadBodyAsync(
                httpResponse.Content,
                cancellationToken);

            problemDetails = BuildProblemDetails(
                responseMessage,
                statusCode,
                absolutePath,
                out additionalData);

            SetProblemDetailsForServerError(problemDetails, absolutePath, clientName);

            logger.LogError(
                "Request call failed.{nl}" +
                "{RequestInfo}{nl2}" +
                "\tResponse={Response}",
                Environment.NewLine,
                requestInfo,
                Environment.NewLine,
                responseMessage);
        }
        catch (OperationCanceledException ex)
        {
            canceledException = ex;
        }
        catch (Exception ex)
        {
            innerException = ex.InnerException ?? ex;
            additionalData = null;

            logger.LogError(
                innerException,
                "Failed to process downstream error response.{nl}{RequestInfo}",
                Environment.NewLine,
                requestInfo);

            problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Instance = absolutePath,
                Title = DefaultProblemTitle,
                Detail = GetDefaultProblemDetailsMessage(clientName, absolutePath),
            };
        }

        if (canceledException is not null)
        {
            throw canceledException;
        }

        throw new ProxiedException(
            problemDetails ??
            throw new InvalidOperationException(
                $"{nameof(ProxiedResponseValidator)}: problemDetails was not initialized."),
            statusCode,
            innerException,
            additionalData);
    }

    private static string SerializeForLog(object? logContent) =>
        logContent is null ? "null" : JsonSerializer.Serialize(logContent, LogJsonOptions);

    private static string GetDefaultProblemDetailsMessage(string clientName, string absolutePath)
    {
        return $"Не удалось корректно обработать запрос по адресу '{absolutePath}' для клиента '{clientName}'.";
    }

    private static Dictionary<string, object>? TakeAdditionalData(
        ProblemDetails problemDetails)
    {
        if (!problemDetails.Extensions.Remove(AdditionalDataPropertyName, out var data))
        {
            return null;
        }

        return data switch
        {
            JsonElement { ValueKind: JsonValueKind.Object } jsonElement
                => jsonElement
                    .EnumerateObject()
                    .ToDictionary(x => x.Name, x => x.Value as object),

            // При ручном конструировании ProblemDetails в тестах данные могут быть уже десериализованы.
            Dictionary<string, object> dict => dict,

            IReadOnlyDictionary<string, object> readOnlyDict
                => readOnlyDict.ToDictionary(x => x.Key, x => x.Value),

            _ => null,
        };
    }

    private static ProblemDetails BuildProblemDetails(
        string responseMessage,
        int statusCode,
        string absolutePath,
        out IReadOnlyDictionary<string, object>? additionalData)
    {
        var problemDetails = JsonHelper.TryDeserialize<ProblemDetails>(responseMessage, out var deserializedProblemDetails)
            ? deserializedProblemDetails
            : new ProblemDetails
            {
                Status = statusCode,
                Instance = absolutePath,
                Detail = responseMessage,
            };

        additionalData = TakeAdditionalData(problemDetails);
        return problemDetails;
    }

    private static void SetProblemDetailsForServerError(
        ProblemDetails problemDetails,
        string absolutePath,
        string clientName)
    {
        if (!problemDetails.Extensions.TryGetValue(ErrorsPropertyName, out var errorsJsonElement) ||
            errorsJsonElement is not JsonElement { ValueKind: JsonValueKind.Array } errorsArray)
        {
            return;
        }

        var errorFound = false;
        var errorDetails = string.Empty;
        foreach (var element in errorsArray.EnumerateArray())
        {
            if (!element.TryGetProperty(StatusPropertyName, out var statusCodeProperty) ||
                !statusCodeProperty.TryGetInt32(out var statusCode) ||
                statusCode != StatusCodes.Status500InternalServerError)
            {
                continue;
            }

            errorFound = true;
            errorDetails = element.TryGetProperty(DetailPropertyName, out var detailProp)
                ? detailProp.GetString()
                : string.Empty;
            break;
        }

        if (!errorFound)
        {
            return;
        }

        problemDetails.Title = DefaultProblemTitle;
        problemDetails.Detail =
            GetDefaultProblemDetailsMessage(clientName, absolutePath) +
            (string.IsNullOrWhiteSpace(errorDetails)
                ? string.Empty
                : $"{Environment.NewLine}Причина: {errorDetails}");
    }

    private static string ResolveAbsolutePath(HttpResponseMessage response, string? logUri)
    {
        var candidate =
            response.RequestMessage?.RequestUri?.AbsolutePath ??
            logUri ??
            string.Empty;
        return string.IsNullOrWhiteSpace(candidate) || candidate == "/"
            ? UnknownPathPlaceholder
            : candidate;
    }

    private async Task<string> ReadBodyAsync(
        HttpContent content,
        CancellationToken cancellationToken)
    {
        var body = await content.ReadAsStringAsync(cancellationToken);
        if (body.Length <= _settings.MaxLoggedBodyLength)
        {
            return body;
        }

        return body[.._settings.MaxLoggedBodyLength]
            + $"…[truncated, total {body.Length} chars]";
    }
}
