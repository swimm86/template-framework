// ----------------------------------------------------------------------------------------------
// <copyright file="ProxiedResponseValidator.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.ApiClient.Interfaces;
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
/// </remarks>
/// <param name="logger">Логгер.</param>
public sealed class ProxiedResponseValidator(
    ILogger<ProxiedResponseValidator> logger)
    : IResponseValidator
{
    private static readonly string ErrorsPropertyName = nameof(ErrorResponse.Errors).ToCamelCase();
    private static readonly string DetailPropertyName = nameof(ProblemDetails.Detail).ToCamelCase();
    private static readonly string StatusPropertyName = nameof(ProblemDetails.Status).ToCamelCase();
    private static readonly string AdditionalDataPropertyName = nameof(IWithAdditionalData.AdditionalData).ToCamelCase();

    /// <inheritdoc />
    public async Task ValidateAsync(
        HttpResponseMessage httpResponse,
        string clientName,
        string? logUri = null,
        object? logContent = null,
        CancellationToken cancellationToken = default)
    {
        if (httpResponse.IsSuccessStatusCode)
        {
            return;
        }

        var isThrown = false;
        try
        {
            var absolutePath =
                httpResponse.RequestMessage?.RequestUri?.AbsolutePath
                ?? logUri
                ?? string.Empty;

            var response = await httpResponse.Content
                .ReadAsStringAsync(cancellationToken);
            var problemDetails = JsonHelper.TryDeserialize<ProblemDetails>(response, out var deserializedProblemDetails)
                ? deserializedProblemDetails
                : new ProblemDetails
                {
                    Status = (int)httpResponse.StatusCode,
                    Instance = absolutePath,
                    Detail = response,
                };

            var additionalData = TakeAdditionalData(problemDetails!);
            if (logUri != null && logContent != null)
            {
                logger.LogError(
                    "Запрос по адресу {RequestUri} вернул ошибку: Status Code={StatusCode:D} с телом {ResponseBody}",
                    absolutePath,
                    httpResponse.StatusCode,
                    JsonSerializer.Serialize(logContent));
            }

            SetProblemDetailsForServerError(problemDetails, absolutePath, clientName);

            isThrown = true;
            throw new ProxiedException(problemDetails, (int)httpResponse.StatusCode, additionalData);
        }
        catch (OperationCanceledException)
        {
            isThrown = true;
            throw;
        }
        finally
        {
            if (!isThrown)
            {
                throw await CreateExceptionAsync(httpResponse, clientName, cancellationToken);
            }
        }
    }

    private static async Task<Exception> CreateExceptionAsync(
        HttpResponseMessage response,
        string clientName,
        CancellationToken cancellationToken)
    {
        if (response == null)
        {
            throw new ArgumentNullException(nameof(response), "Ответ HTTP не может быть null.");
        }

        try
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var errorMessage = $"Запрос к {clientName} завершился ошибкой: " +
                               $"Status Code={response.StatusCode:D} ({response.ReasonPhrase}), " +
                               $"Content=[{content ?? "null"}]";
            return new Exception(errorMessage);
        }
        catch (Exception ex)
        {
            return new Exception(
                $"Не удалось прочитать содержимое ошибки HTTP. Status Code={response.StatusCode:D} ({response.ReasonPhrase})",
                ex);
        }
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

        problemDetails.Title = "Ошибка во время взаимодействия с внешним сервисом.";
        problemDetails.Detail =
            $"Не удалось корректно обработать запрос по адресу '{absolutePath}' для клиента '{clientName}'." +
            (string.IsNullOrWhiteSpace(errorDetails)
                ? string.Empty
                : $"{Environment.NewLine}Причина: {errorDetails}");
    }
}
