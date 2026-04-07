// ----------------------------------------------------------------------------------------------
// <copyright file="IPropertySetter.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

namespace Shared.Domain.Core.Utils.Interfaces;

/// <summary>
/// Интерфейс для установления значений свойств из объектов.
/// </summary>
public interface IPropertySetter
{
    /// <summary>
    /// Устанавливает значение свойства объекта.
    /// </summary>
    /// <param name="obj">Объект, которому необходимо установить значение свойства.</param>
    /// <param name="propertyName">Название свойства, в которое необходимо установить значение.</param>
    /// <param name="value">Значение.</param>
    /// <param name="throwIfNotFound">
    /// Если <c>true</c> (по умолчанию) — выбрасывает <see cref="InvalidOperationException"/>, когда свойство не найдено.
    /// Если <c>false</c> — операция игнорируется молча: исключение не выбрасывается, значение не устанавливается.
    /// </param>
    void SetProperty(
        object obj,
        string propertyName,
        object? value,
        bool throwIfNotFound = true);
}