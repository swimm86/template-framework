// ----------------------------------------------------------------------------------------------
// <copyright file="IPersonsService.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Gpn.Template.Domain.Entities;

namespace Gpn.Template.Getter.Application.Interfaces;

/// <summary>
/// Интерфейс тестового сервиса.
/// </summary>
public interface IPersonsService
{
    /// <summary>
    /// Получить коллекцию всех person-ов.
    /// </summary>
    /// <returns>Коллекцию person-ов.</returns>
    List<Person> GetPersons();
}
