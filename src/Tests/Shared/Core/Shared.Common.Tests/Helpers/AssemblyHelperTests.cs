// ----------------------------------------------------------------------------------------------
// <copyright file="AssemblyHelperTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Reflection;
using Shared.Common.Helpers;

[assembly: Shared.Common.Tests.Helpers.AssemblyMarker("AssemblyMarker")]

namespace Shared.Common.Tests.Helpers;

/// <summary>
/// Тестовый атрибут для проверки поиска атрибутов сборки.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
internal sealed class AssemblyMarkerAttribute(string value)
    : Attribute
{
    public string Value { get; } = value;
}

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
    private class TestMarkerAttribute(string? value = null)
        : Attribute
    {
        public string? Value { get; } = value;
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

    /// <summary>
    /// Производный generic-тип для проверки поиска сборок по базовому generic-типу.
    /// </summary>
    private class TestStringList : List<string>
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
        result.Should().Be(assembly.GetName().Name);
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
        result.Should().NotBeEmpty()
            .And.OnlyContain(a => a.GetName().Name != null && a.GetName().Name!.StartsWith(prefix));
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
        result.Should().NotBeEmpty();

        var expectedPrefix = entryAssembly.GetName().Name!.Split('.').First();
        result.Should().OnlyContain(a => a.GetName().Name != null && a.GetName().Name!.StartsWith(expectedPrefix));
    }

    #endregion

    #region GetAttributesFromAssemblies Tests

    /// <summary>
    /// Проверяет получение атрибутов указанного типа из сборок.
    /// </summary>
    [Fact]
    public void GetAttributesFromAssemblies_FindsAssemblyAttributesInCurrentAssembly()
    {
        // Act
        var result = AssemblyHelper.GetAttributesFromAssemblies<AssemblyMarkerAttribute>(
            typeof(AssemblyHelperTests).Assembly);

        // Assert
        result.Should().Contain(attribute => attribute.Value == "AssemblyMarker");
    }

    /// <summary>
    /// Проверяет, что метод возвращает пустую коллекцию, если атрибуты не найдены.
    /// </summary>
    [Fact]
    public void GetAttributesFromAssemblies_NoMatchingAttributes_ReturnsEmptyCollection()
    {
        // Act
        var result = AssemblyHelper.GetAttributesFromAssemblies<ObsoleteAttribute>(
            typeof(AssemblyHelperTests).Assembly);

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Проверяет получение атрибутов указанного типа из типов внутри сборок.
    /// </summary>
    [Fact]
    public void GetTypeAttributesFromAssemblies_FindsCustomAttributesInCurrentAssembly()
    {
        // Act
        var result = AssemblyHelper.GetTypeAttributesFromAssemblies<TestMarkerAttribute>(
            typeof(AssemblyHelperTests).Assembly);

        // Assert
        result.Select(attribute => attribute.Value).Should()
            .Contain(["TestClass1", "Derived", "InterfaceImpl"]);
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
            includedAttributesTypes: [typeof(TestMarkerAttribute)]);

        // Assert
        result.Should().Contain(typeof(DerivedClass));
    }

    /// <summary>
    /// Проверяет поиск реализаций интерфейса.
    /// </summary>
    [Fact]
    public void GetDerivedTypesFromAssemblies_FindsInterfaceImplementations()
    {
        // Act
        var result = AssemblyHelper.GetDerivedTypesFromAssemblies<ITestInterface>(
            includedAttributesTypes: [typeof(TestMarkerAttribute)]);

        // Assert
        result.Should().Contain(typeof(InterfaceImplementation));
    }

    /// <summary>
    /// Проверяет фильтрацию по включаемым атрибутам.
    /// </summary>
    [Fact]
    public void GetDerivedTypesFromAssemblies_WithIncludedAttributes_FiltersCorrectly()
    {
        // Act
        var result = AssemblyHelper.GetDerivedTypesFromAssemblies<BaseClass>(
            includedAttributesTypes: [typeof(TestMarkerAttribute)]);

        // Assert
        // Должен содержать только классы с атрибутом TestMarkerAttribute
        result.Should().OnlyContain(t => t.GetCustomAttribute<TestMarkerAttribute>() != null);
    }

    /// <summary>
    /// Проверяет фильтрацию по исключаемым атрибутам.
    /// </summary>
    [Fact]
    public void GetDerivedTypesFromAssemblies_WithExcludedAttributes_FiltersCorrectly()
    {
        // Act
        var result = AssemblyHelper.GetDerivedTypesFromAssemblies<BaseClass>(
            excludedAttributesTypes: [typeof(TestMarkerAttribute)]);

        // Assert
        // Не должен содержать классы с атрибутом TestMarkerAttribute
        result.Should().NotContain(t => t.GetCustomAttribute<TestMarkerAttribute>() != null);
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
        result.Should().NotContain(t => t.IsAbstract);
    }

    /// <summary>
    /// Проверяет обобщенный вариант метода <see cref="AssemblyHelper.GetDerivedTypesFromAssemblies{TType}(Type[], Type[])"/>.
    /// </summary>
    [Fact]
    public void GetDerivedTypesFromAssemblies_GenericOverload_FindsDerivedTypes()
    {
        // Act
        var result = AssemblyHelper.GetDerivedTypesFromAssemblies<BaseClass>(
            includedAttributesTypes: [typeof(TestMarkerAttribute)]);

        // Assert
        result.Should().Contain(typeof(DerivedClass));
    }

    #endregion

    #region GetAssembliesContainingDerivedGenericTypes Tests

    /// <summary>
    /// Проверяет получение сборок, содержащих типы, производные от обобщенного базового типа.
    /// </summary>
    [Fact]
    public void GetAssembliesContainingDerivedGenericTypes_FindsAssembliesWithDerivedTypes()
    {
        // Act
        var result = AssemblyHelper.GetAssembliesContainingDerivedGenericTypes(
            typeof(List<>),
            typeof(TestStringList).Assembly);

        // Assert
        result.Should().Contain(typeof(TestStringList).Assembly);
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
        result.Should().NotBeNull();
        result!.FullName.Should().Be(targetAssembly.FullName);
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
        result.Should().BeNull();
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
        var result = AssemblyHelper.GetModuleName(entryAssembly: null);

        // Assert
        result.Should().NotBeNull();
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
        result.Should().NotBeNull();
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
            includedAttributesTypes: [],
            excludedAttributesTypes: []);

        // Assert
        result.Should().Contain(typeof(DerivedClass));
    }

    #endregion
}
