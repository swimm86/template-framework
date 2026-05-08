// ----------------------------------------------------------------------------------------------
// <copyright file="ApiClientPrimaryHttpMessageHandlerMetadataAttribute.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.ApiClient.Handlers.Attributes.Base;

namespace Shared.Application.Core.ApiClient.Handlers.Attributes;

/// <summary>
/// Задает метаданные для primary HTTP message handler-ов.
/// </summary>
/// <inheritdoc cref="ApiClientHandlerMetadataAttributeBase"/>
public sealed class ApiClientPrimaryHttpHandlerMetadataAttribute(
    params Type[] clientTypes)
    : ApiClientHandlerMetadataAttributeBase(clientTypes);