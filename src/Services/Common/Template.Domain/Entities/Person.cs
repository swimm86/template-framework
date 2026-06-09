// ----------------------------------------------------------------------------------------------
// <copyright file="Person.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Common.Helpers;
using Shared.Domain.Core.Base;

namespace Template.Domain.Entities;

/// <summary>
/// Сущность "Персона".
/// </summary>
public class Person
    : EntityBase<Guid>
{
    /// <summary>
    /// Имя.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Адрес электронной почты.
    /// </summary>
    public string Email { get; private set; }

    /// <summary>
    /// Хэш.
    /// </summary>
    public byte[] Hash { get; private set; }

    /// <summary>
    /// Создает сущность "Персона".
    /// </summary>
    /// <param name="name"><inheritdoc cref="Domain.Entities.Person.Name" path="/summary"/></param>
    /// <param name="email"><inheritdoc cref="Domain.Entities.Person.Email" path="/summary"/></param>
    /// <returns>Экземпляр сущности "Персона".</returns>
    public static Person Create(
        string name,
        string email)
    {
        return new Person
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
        };
    }

    /// <summary>
    /// Обновляет хэш сущности на основе текущих значений <see cref="Name"/> и <see cref="Email"/>.
    /// </summary>
    /// <remarks>
    /// Хэш используется для обеспечения уникальности персоны и вычисляется детерминированно:
    /// одинаковые пары <c>(Name, Email)</c> всегда дают идентичный массив байт.
    /// </remarks>
    public void UpdateHash()
    {
        Hash = HashHelper.ComputeSha256(Name, Email);
    }
}
