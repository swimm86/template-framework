// ----------------------------------------------------------------------------------------------
// <copyright file="AssemblyHelper.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Reflection;

namespace Shared.Common.Helpers;

/// <summary>
/// Предоставляет вспомогательные методы общего назначения.
/// </summary>
public static class AssemblyHelper
{
    /// <summary>
    /// Возвращает название модуля, представляющее собой имя сборки, содержащей точку входа в приложение.
    /// </summary>
    /// <param name="entryAssembly">Сборка для получения имени модуля. Если не указана, используется сборка точки входа в приложение.</param>
    /// <returns>Имя сборки, которая была определена как точка входа в приложение.</returns>
    public static string GetModuleName(Assembly? entryAssembly = default) =>
        (entryAssembly ?? Assembly.GetEntryAssembly())!.GetName().Name!;

    /// <summary>
    /// Возвращает перечисление сборок, загруженных в текущий домен приложения, которые соответствуют заданному префиксу.
    /// </summary>
    /// <param name="entryAssembly">Сборка, используемая для определения префикса по умолчанию, если таковой не указан. Если не указана, используется сборка точки входа в приложение.</param>
    /// <param name="prefix">Префикс имени сборки, который используется для фильтрации сборок. Если не указан, используется первая часть имени сборки точки входа, разделённая точкой.</param>
    /// <returns>Перечисление сборок, соответствующих заданному префиксу.</returns>
    public static IEnumerable<Assembly> GetAssembliesByPrefix(
        Assembly? entryAssembly = default,
        string? prefix = default)
    {
        entryAssembly ??= Assembly.GetEntryAssembly()!;
        prefix ??= entryAssembly.GetName().Name!.Split('.').First();
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => assembly.GetName().Name?.StartsWith(prefix) ?? false);
    }

    /// <summary>
    /// Возвращает перечисление сборок, содержащих типы, унаследованные от указанного обобщенного базового типа.
    /// </summary>
    /// <param name="type">Тип обобщенного базового класса для поиска производных типов.</param>
    /// <param name="entryAssembly">Сборка, с которой начинается поиск. Если не указано, используется сборка, в которой находится вызывающий код.</param>
    /// <returns>Перечисление сборок, содержащих типы, производные от указанного обобщенного базового типа.</returns>
    public static IEnumerable<Assembly> GetAssembliesContainingDerivedGenericTypes(
        Type type,
        Assembly? entryAssembly = default)
    {
        // Получаем все сборки по заданному префиксу
        var assemblies = GetAssembliesByPrefix(entryAssembly);

        // Фильтруем сборки, содержащие типы, наследующиеся от указанного обобщенного типа
        return assemblies
            .Where(assembly =>
                assembly.GetTypes().Any(t =>
                    t.BaseType is { IsGenericType: true } &&
                    t.BaseType.GetGenericTypeDefinition() == type));
    }

    /// <summary>
    /// Получает все типы, производные от указанного типа, из всех загруженных сборок.
    /// </summary>
    /// <typeparam name="TType">Тип, для которого необходимо найти производные типы.</typeparam>
    /// <param name="includedAttributesTypes">Список типов аттрибутов, которые должны содержать целевые типы.</param>
    /// <param name="excludedAttributesTypes">Список типов аттрибутов, которые не должны содержать целевые типы.</param>
    /// <returns>Перечисление производных типов.</returns>
    public static IEnumerable<Type> GetDerivedTypesFromAssemblies<TType>(
        Type[]? includedAttributesTypes = default,
        Type[]? excludedAttributesTypes = default)
    {
        return GetDerivedTypesFromAssemblies(typeof(TType));
    }

    /// <summary>
    /// Получает все типы, производные от указанного типа, из всех загруженных сборок.
    /// </summary>
    /// <param name="baseType">Тип, для которого необходимо найти производные типы.</param>
    /// <param name="includedAttributesTypes">Список типов аттрибутов, которые должны содержать целевые типы.</param>
    /// <param name="excludedAttributesTypes">Список типов аттрибутов, которые не должны содержать целевые типы.</param>
    /// <returns>Перечисление производных типов.</returns>
    public static IEnumerable<Type> GetDerivedTypesFromAssemblies(
        Type baseType,
        Type[]? includedAttributesTypes = default,
        Type[]? excludedAttributesTypes = default)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(type =>
            {
                if (type is not { IsClass: true, IsAbstract: false })
                {
                    return false;
                }

                if (includedAttributesTypes != null &&
                    includedAttributesTypes.Any(attrType => type.GetCustomAttribute(attrType) == null))
                {
                    return false;
                }

                if (excludedAttributesTypes != null &&
                    excludedAttributesTypes.Any(attrType => type.GetCustomAttribute(attrType) != null))
                {
                    return false;
                }

                var interfaces = type.GetInterfaces();
                if (interfaces.Any(i => i == baseType || (i.IsGenericType && i.GetGenericTypeDefinition() == baseType)))
                {
                    return true;
                }

                // Если baseType не интерфейс, проверяем, что type наследуется от baseType
                return !baseType.IsInterface && baseType.IsAssignableFrom(type);
            })
            .Distinct();
    }
}
