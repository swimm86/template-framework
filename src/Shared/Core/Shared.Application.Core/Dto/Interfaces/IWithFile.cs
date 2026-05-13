// ----------------------------------------------------------------------------------------------
// <copyright file="IWithFile.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;

namespace Shared.Application.Core.Dto.Interfaces;

/// <summary>
/// Интерфейс с файлом.
/// </summary>
public interface IWithFile
{
    /// <summary>
    /// Файл.
    /// </summary>
    public IFormFile? File { get; }
}
