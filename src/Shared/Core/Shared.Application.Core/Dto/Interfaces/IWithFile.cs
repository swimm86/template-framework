// ----------------------------------------------------------------------------------------------
// <copyright file="IWithFile.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
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
