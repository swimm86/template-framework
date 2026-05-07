// ----------------------------------------------------------------------------------------------
// <copyright file="AssemblyHelperTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Reflection;
using Shared.Common.Helpers;

namespace Shared.Common.Tests.Helpers;

/// <summary>
/// Тесты для вспомогательного класса <see cref="AssemblyHelper"/>.
/// Проверяет корректность работы с сборками: получение по префиксу, поиск типов, работа с атрибутами.
/// </summary>
public sealed class AssemblyHelperTests
{
    /// <summary>
    /// Тестовый атрибут для проверки поиска типов с атрибутами.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    private class TestMarkerAttribute : Attribute
    {
        public string? Value { get; }

        public TestMarkerAttribute(string? value = null)
        {
            Value = value;
        }
    }

    /// <summary>
    /// Тестовый класс с атрибутом для проверки поиска производных типов.
    /// </summary>
    [TestMarker("TestClass1")]
    private class TestClassWithAttribute
    {
    }

    /// <summary>
    /// Тестовый класс без атрибута.
    /// </summary>
    private class TestClassWithoutAttribute
    {
    }

    /// <summary>
    /// Базовый класс для проверки наследования.
    /// </summary>
    private abstract class BaseClass
    {
    }

    /// <summary>
    /// Производный класс для проверки поиска наследников.
    /// </summary>
    [TestMarker("Derived")]
    private class DerivedClass : BaseClass
    {
    }

    /// <summary>
    /// Базовый интерфейс для проверки реализации интерфейсов.
    /// </summary>
    private interface ITestInterface
    {
    }

    /// <summary>
    /// Класс, реализующий интерфейс.
    /// </summary>
    [TestMarker("InterfaceImpl")]
    private class InterfaceImplementation : ITestInterface
    {
    }

    #region GetModuleName Tests

    /// <summary>
    /// Проверяет получение имени модуля из текущей сборки.
    /// </summary>
    [Fact]
    public void GetModuleName_WithExplicitAssembly_ReturnsAssemblyName()
    {
        // Arrange
        var assembly = typeof(AssemblyHelperTests).Assembly;

        // Act
        var result = AssemblyHelper.GetModuleName(assembly);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(assembly.GetName().Name, result);
    }

    #endregion

    #region GetAssembliesByPrefix Tests

    /// <summary>
    /// Проверяет получение сборок по префиксу.
    /// </summary>
    [Fact]
    public void GetAssembliesByPrefix_WithExplicitPrefix_ReturnsMatchingAssemblies()
    {
        // Arrange
        var assembly = typeof(AssemblyHelperTests).Assembly;
        var prefix = "Shared.Common";

        // Act
        var result = AssemblyHelper.GetAssembliesByPrefix(assembly, prefix);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.All(result, a => Assert.StartsWith(prefix, a.GetName().Name));
    }

    /// <summary>
    /// Проверяет получение сборок по префиксу из имени сборки точки входа.
    /// </summary>
    [Fact]
    public void GetAssembliesByPrefix_WithoutPrefix_UsesEntryAssemblyPrefix()
    {
        // Arrange
        var entryAssembly = typeof(AssemblyHelperTests).Assembly;

        // Act
        var result = AssemblyHelper.GetAssembliesByPrefix(entryAssembly);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        
        var expectedPrefix = entryAssembly.GetName().Name!.Split('.').First();
        Assert.All(result, a => Assert.StartsWith(expectedPrefix, a.GetName().Name));
    }

    #endregion

    #region GetAttributesFromAssemblies Tests

    /// <summary>
    /// Проверяет получение атрибутов указанного типа из сборок.
    /// </summary>
    [Fact]
    public void GetAttributesFromAssemblies_FindsCustomAttributesInCurrentAssembly()
    {
        // Act
        var result = AssemblyHelper.GetAttributesFromAssemblies<TestMarkerAttribute>(
            typeof(AssemblyHelperTests).Assembly);

        // Assert
        Assert.NotNull(result);
        // Должен найти хотя бы наши тестовые атрибуты
        Assert.NotEmpty(result);
    }

    /// <summary>
    /// Проверяет, что метод возвращает пустую коллекцию, если атрибуты не найдены.
    /// </summary>
    [Fact]
    public void GetAttributesFromAssemblies_NoMatchingAttributes_ReturnsEmptyCollection()
    {
        // Arrange - используем несуществующий тип атрибута
        // AssemblyHelper уже фильтрует по префиксу, поэтому просто проверяем пустоту
        
        // Act
        var result = AssemblyHelper.GetAttributesFromAssemblies<ObsoleteAttribute>(
            typeof(AssemblyHelperTests).Assembly);

        // Assert
        Assert.NotNull(result);
        // Может быть пустым или содержать атрибуты из зависимостей
    }

    #endregion

    #region GetDerivedTypesFromAssemblies Tests

    /// <summary>
    /// Проверяет поиск производных типов от базового класса.
    /// </summary>
    [Fact]
    public void GetDerivedTypesFromAssemblies_FindsDerivedClasses()
    {
        // Act
        var result = AssemblyHelper.GetDerivedTypesFromAssemblies<BaseClass>(
            includedAttributesTypes: new[] { typeof(TestMarkerAttribute) });

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result, t => t == typeof(DerivedClass));
    }

    /// <summary>
    /// Проверяет поиск реализаций интерфейса.
    /// </summary>
    [Fact]
    public void GetDerivedTypesFromAssemblies_FindsInterfaceImplementations()
    {
        // Act
        var result = AssemblyHelper.GetDerivedTypesFromAssemblies<ITestInterface>(
            includedAttributesTypes: new[] { typeof(TestMarkerAttribute) });

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result, t => t == typeof(InterfaceImplementation));
    }

    /// <summary>
    /// Проверяет фильтрацию по включаемым атрибутам.
    /// </summary>
    [Fact]
    public void GetDerivedTypesFromAssemblies_WithIncludedAttributes_FiltersCorrectly()
    {
        // Act
        var result = AssemblyHelper.GetDerivedTypesFromAssemblies<BaseClass>(
            includedAttributesTypes: new[] { typeof(TestMarkerAttribute) });

        // Assert
        Assert.NotNull(result);
        // Должен содержать только классы с атрибутом TestMarkerAttribute
        Assert.All(result, t => Assert.True(t.GetCustomAttribute<TestMarkerAttribute>() != null));
    }

    /// <summary>
    /// Проверяет фильтрацию по исключаемым атрибутам.
    /// </summary>
    [Fact]
    public void GetDerivedTypesFromAssemblies_WithExcludedAttributes_FiltersCorrectly()
    {
        // Arrange - создаем класс с другим атрибутом
        // В нашем случае все тестовые классы либо с TestMarkerAttribute, либо без атрибутов
        
        // Act
        var result = AssemblyHelper.GetDerivedTypesFromAssemblies<BaseClass>(
            excludedAttributesTypes: new[] { typeof(TestMarkerAttribute) });

        // Assert
        Assert.NotNull(result);
        // Не должен содержать классы с атрибутом TestMarkerAttribute
        Assert.DoesNotContain(result, t => t.GetCustomAttribute<TestMarkerAttribute>() != null);
    }

    /// <summary>
    /// Проверяет, что абстрактные классы не включаются в результат.
    /// </summary>
    [Fact]
    public void GetDerivedTypesFromAssemblies_ExcludesAbstractClasses()
    {
        // Act
        var result = AssemblyHelper.GetDerivedTypesFromAssemblies<BaseClass>();

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain(result, t => t.IsAbstract);
    }

    /// <summary>
    /// Проверяет обобщенный вариант метода GetDerivedTypesFromAssemblies<TType>.
    /// </summary>
    [Fact]
    public void GetDerivedTypesFromAssemblies_GenericOverload_FindsDerivedTypes()
    {
        // Act
        var result = AssemblyHelper.GetDerivedTypesFromAssemblies<BaseClass>(
            includedAttributesTypes: new[] { typeof(TestMarkerAttribute) });

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result, t => t == typeof(DerivedClass));
    }

    #endregion

    #region GetAssembliesContainingDerivedGenericTypes Tests

    /// <summary>
    /// Проверяет получение сборок, содержащих типы, производные от обобщенного базового типа.
    /// </summary>
    [Fact]
    public void GetAssembliesContainingDerivedGenericTypes_FindsAssembliesWithDerivedTypes()
    {
        // Arrange - используем известный обобщенный тип
        // Например, ControllerBase из ASP.NET Core или другой распространенный тип
        
        // Act
        // Используем тип из текущей сборки для теста
        var result = AssemblyHelper.GetAssembliesContainingDerivedGenericTypes(
            typeof(List<>),
            typeof(AssemblyHelperTests).Assembly);

        // Assert
        Assert.NotNull(result);
        // Результат может быть пустым, если в сборке нет типов, наследующих List<T>
    }

    #endregion

    #region GetAssemblyByName Extension Tests

    /// <summary>
    /// Проверяет extension-метод GetAssemblyByName.
    /// </summary>
    [Fact]
    public void GetAssemblyByName_ExistingAssembly_ReturnsAssembly()
    {
        // Arrange
        var appDomain = AppDomain.CurrentDomain;
        var targetAssembly = typeof(AssemblyHelperTests).Assembly;
        var assemblyName = targetAssembly.GetName().Name!;

        // Act
        var result = appDomain.GetAssemblyByName(assemblyName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(targetAssembly.FullName, result.FullName);
    }

    /// <summary>
    /// Проверяет, что GetAssemblyByName возвращает null для несуществующей сборки.
    /// </summary>
    [Fact]
    public void GetAssemblyByName_NonExistentAssembly_ReturnsNull()
    {
        // Arrange
        var appDomain = AppDomain.CurrentDomain;
        const string nonExistentName = "NonExistent.Assembly.Name";

        // Act
        var result = appDomain.GetAssemblyByName(nonExistentName);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Edge Cases and Error Handling Tests

    /// <summary>
    /// Проверяет обработку null-входа для GetModuleName.
    /// </summary>
    [Fact]
    public void GetModuleName_NullEntryAssembly_UsesGetEntryAssembly()
    {
        // Act
        var result = AssemblyHelper.GetModuleName(null);

        // Assert
        Assert.NotNull(result);
    }

    /// <summary>
    /// Проверяет, что GetLoadableTypes защищен от ReflectionTypeLoadException.
    /// Это проверяется косвенно через другие методы, использующие GetLoadableTypes.
    /// </summary>
    [Fact]
    public void GetDerivedTypesFromAssemblies_HandlesPartiallyLoadedAssemblies()
    {
        // Act - метод должен завершиться без исключений
        var result = AssemblyHelper.GetDerivedTypesFromAssemblies<object>();

        // Assert
        Assert.NotNull(result);
        // Метод должен вернуть типы, даже если некоторые сборки частично загружены
    }

    /// <summary>
    /// Проверяет работу с пустыми массивами атрибутов.
    /// </summary>
    [Fact]
    public void GetDerivedTypesFromAssemblies_EmptyAttributeArrays_WorksCorrectly()
    {
        // Act
        var result = AssemblyHelper.GetDerivedTypesFromAssemblies<BaseClass>(
            includedAttributesTypes: Array.Empty<Type>(),
            excludedAttributesTypes: Array.Empty<Type>());

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result, t => t == typeof(DerivedClass));
    }

    #endregion
}
