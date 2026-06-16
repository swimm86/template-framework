// ----------------------------------------------------------------------------------------------
// <copyright file="AssemblyHelper.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Reflection;
using Shared.Common.Attributes;

namespace Shared.Common.Helpers;

/// <summary>
/// Предоставляет вспомогательные методы для <see cref="Assembly"/>.
/// </summary>
public static class AssemblyHelper
{
    private static readonly Func<Assembly?> ResolveStartupAssembly = () =>
        AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(a => a.IsDefined(typeof(StartupAssemblyAttribute), false))
        ?? Assembly.GetEntryAssembly();

    private static Lazy<Assembly?> _startupAssemblyCache = new(
        ResolveStartupAssembly,
        LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Возвращает название модуля, представляющее собой имя сборки, содержащей точку входа в приложение.
    /// </summary>
    /// <param name="entryAssembly">Сборка для получения имени модуля. Если не указана, используется сборка точки входа в приложение.</param>
    /// <returns>Имя сборки, которая была определена как точка входа в приложение.</returns>
    public static string GetModuleName(Assembly? entryAssembly = null)
    {
        var result = (entryAssembly ?? _startupAssemblyCache.Value)!.GetName().Name!;
        return result;
    }

    /// <summary>
    /// Возвращает перечисление сборок, загруженных в текущий домен приложения, которые соответствуют заданному префиксу.
    /// </summary>
    /// <param name="entryAssembly">Сборка, используемая для определения префикса по умолчанию, если таковой не указан. Если не указана, используется сборка точки входа в приложение.</param>
    /// <param name="prefix">Префикс имени сборки, который используется для фильтрации сборок. Если не указан, используется первая часть имени сборки точки входа, разделённая точкой.</param>
    /// <returns>Перечисление сборок, соответствующих заданному префиксу.</returns>
    public static IEnumerable<Assembly> GetAssembliesByPrefix(
        Assembly? entryAssembly = null,
        string? prefix = null)
    {
        entryAssembly ??= Assembly.GetEntryAssembly()!;
        prefix ??= entryAssembly.GetName().Name!.Split('.').First();
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => assembly.GetName().Name?.StartsWith(prefix) ?? false);
    }

    /// <summary>
    /// Возвращает перечисление сборок, содержащих типы, унаследованные от указанного обобщенного базового типа.
    /// </summary>
    /// <param name="type">Обобщённый базовый тип для поиска производных типов.</param>
    /// <param name="entryAssembly">Сборка, с которой начинается поиск. Если не указано, используется сборка, в которой находится вызывающий код.</param>
    /// <returns>Перечисление сборок, содержащих типы, производные от указанного обобщенного базового типа.</returns>
    public static IEnumerable<Assembly> GetAssembliesContainingDerivedGenericTypes(
        Type type,
        Assembly? entryAssembly = null)
    {
        // Получаем все сборки по заданному префиксу
        var assemblies = GetAssembliesByPrefix(entryAssembly);

        // Фильтруем сборки, содержащие типы, наследующиеся от указанного обобщенного типа
        return assemblies
            .Where(assembly =>
                GetLoadableTypes(assembly).Any(t =>
                    t.BaseType is { IsGenericType: true } &&
                    t.BaseType.GetGenericTypeDefinition() == type));
    }

    /// <summary>
    /// Получает все атрибуты указанного типа из сборок.
    /// </summary>
    /// <typeparam name="TAttribute">Тип атрибута.</typeparam>
    /// <param name="entryAssembly">Сборка, относительно которой происходит поиск. Если не указана, используется текущая сборка.</param>
    /// <returns>Перечисление атрибутов указанного типа из всех найденных сборок.</returns>
    public static IEnumerable<TAttribute> GetAttributesFromAssemblies<TAttribute>(
        Assembly? entryAssembly = null)
        where TAttribute : Attribute
    {
        var assemblies = GetAssembliesByPrefix(entryAssembly);
        return assemblies.SelectMany(a => a.GetCustomAttributes<TAttribute>());
    }

    /// <summary>
    /// Получает все атрибуты указанного типа из типов, объявленных в найденных сборках.
    /// </summary>
    /// <typeparam name="TAttribute">Тип атрибута.</typeparam>
    /// <param name="entryAssembly">Сборка, относительно которой происходит поиск. Если не указана, используется текущая сборка.</param>
    /// <returns>Перечисление атрибутов указанного типа из типов всех найденных сборок.</returns>
    public static IEnumerable<TAttribute> GetTypeAttributesFromAssemblies<TAttribute>(
        Assembly? entryAssembly = null)
        where TAttribute : Attribute
    {
        var assemblies = GetAssembliesByPrefix(entryAssembly);
        return assemblies
            .SelectMany(GetLoadableTypes)
            .Select(t => t.GetCustomAttribute<TAttribute>())
            .Where(attr => attr is not null)!;
    }

    /// <typeparam name="TType">Тип, для которого необходимо найти производные типы.</typeparam>
    /// <returns>Перечисление производных типов.</returns>
    /// <inheritdoc cref="GetDerivedTypesFromAssemblies(Type, Type[], Type[])"/>
    public static IEnumerable<Type> GetDerivedTypesFromAssemblies<TType>(
        Type[]? includedAttributesTypes = null,
        Type[]? excludedAttributesTypes = null)
    {
        return GetDerivedTypesFromAssemblies(
            typeof(TType),
            includedAttributesTypes,
            excludedAttributesTypes);
    }

    /// <summary>
    /// Получает все типы, производные от указанного типа, из всех загруженных сборок.
    /// </summary>
    /// <param name="baseType">Тип, для которого необходимо найти производные типы.</param>
    /// <param name="includedAttributesTypes">Список типов атрибутов, которыми должны быть помечены целевые типы.</param>
    /// <param name="excludedAttributesTypes">Список типов атрибутов, которыми не должны быть помечены целевые типы.</param>
    /// <returns>Перечисление производных типов.</returns>
    public static IEnumerable<Type> GetDerivedTypesFromAssemblies(
        Type baseType,
        Type[]? includedAttributesTypes = null,
        Type[]? excludedAttributesTypes = null)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(GetLoadableTypes)
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

    /// <summary>
    /// Получает сборку по имени из домена приложения.
    /// </summary>
    /// <param name="appDomain">Домен приложения.</param>
    /// <param name="assemblyName">Имя сборки.</param>
    /// <returns>Сборка с указанным именем или <c>null</c>, если не найдена.</returns>
    public static Assembly? GetAssemblyByName(this AppDomain appDomain, string assemblyName)
        => appDomain
            .GetAssemblies()
            .SingleOrDefault(assembly => assembly.GetName().Name == assemblyName);

    /// <summary>
    /// Сбрасывает кеш startup-сборки. Используется в тестах, проверяющих сценарии
    /// с несколькими сборками, помеченными <see cref="StartupAssemblyAttribute"/>,
    /// где порядок загрузки сборок в <see cref="AppDomain"/> влияет на результат.
    /// </summary>
    internal static void ResetStartupAssemblyCache()
    {
        _startupAssemblyCache = new Lazy<Assembly?>(
            ResolveStartupAssembly,
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>
    /// Безопасно получает типы из сборки, защищая от <see cref="ReflectionTypeLoadException"/>
    /// при частично загружаемых сборках (например, с неразрешёнными зависимостями).
    /// </summary>
    /// <param name="assembly">Сборка, из которой необходимо получить типы.</param>
    /// <returns>
    /// Перечисление успешно загруженных типов из указанной сборки.
    /// Если сборка частично загружается, возвращаются только не-<see langword="null"/> типы.
    /// </returns>
    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }
}
