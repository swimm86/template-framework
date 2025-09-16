// ----------------------------------------------------------------------------------------------
// <copyright file="DalDependencyInjector.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Domain.Entities;
using Gpn.Template.Infrastructure.Dal.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.DependencyInjection;
using Shared.Infrastructure.Dal.EFCore.Interfaces;

namespace Gpn.Template.Infrastructure.Dal;

/// <summary>
/// Внедрение зависимостей для DAL-слоя.
/// </summary>
public class DalDependencyInjector(
    ILogger<DalDependencyInjector> logger)
    : DependencyInjectorBase(logger)
{
    /// <inheritdoc />
    protected override IServiceCollection Process(IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddSingleton<ISequenceNumberService<Person>, SequenceNumberService<Person>>()
            .AddSingleton<IBeforeSaveChangesService, BeforeSaveChangesService>();
    }
}
