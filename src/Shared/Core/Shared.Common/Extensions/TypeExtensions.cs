// ----------------------------------------------------------------------------------------------
// <copyright file="TypeExtensions.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Reflection;

namespace Shared.Common.Extensions;

/// <summary>
/// Методы расширения для <see cref="Type"/>.
/// </summary>
public static class TypeExtensions
{
    /// <summary>
    /// Получает <see cref="PropertyInfo"/> типа по наименованию свойства, игнорируя регистр.
    /// </summary>
    /// <param name="type">Тип.</param>
    /// <param name="propertyName">Наименование свойства.</param>
    /// <returns><see cref="PropertyInfo"/> млм <see langword="null"/>, если свойство не было найдено.</returns>
    public static PropertyInfo? GetPropertyIgnoreCase(this Type type, string propertyName) =>
        type.GetProperty(
            propertyName.Trim(),
            BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
}
