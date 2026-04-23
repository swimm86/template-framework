// ----------------------------------------------------------------------------------------------
// <copyright file="CacheNotFoundException.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Domain.Core.Exceptions.Models.Base;

namespace Shared.Application.Core.Cache.Exceptions;

/// <summary>
/// Исключение, возникающее при попытке доступа к отсутствующему кэшу.
/// </summary>
/// <param name="cacheKey">Ключ кэша, который не был найден.</param>
public class CacheNotFoundException(string cacheKey)
    : AppException($"Отсутствует кэш с ключом {cacheKey}.");
