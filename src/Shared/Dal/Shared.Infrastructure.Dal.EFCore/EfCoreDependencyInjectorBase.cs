// ----------------------------------------------------------------------------------------------
// <copyright file="EfCoreDependencyInjectorBase.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.DependencyInjection;
using Shared.Domain.Core.Dal.Repository.Interfaces;
using Shared.Infrastructure.Dal.EFCore.Extensions;
using Shared.Infrastructure.Dal.EFCore.Repository;

namespace Shared.Infrastructure.DAL.EFCore;

/// <summary>
/// Базовый абстрактный класс для внедрения зависимостей EF Core.
/// </summary>
public abstract class EfCoreDependencyInjectorBase(
    ILogger? logger)
    : DependencyInjectorBase(logger)
{
    /// <inheritdoc />
    protected override IServiceCollection Process(IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddSingleton<IQueryEvaluator, EfQueryEvaluator>()
            .AddDbContexts();
    }
}
