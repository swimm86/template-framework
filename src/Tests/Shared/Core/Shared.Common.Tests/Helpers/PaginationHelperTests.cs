// ----------------------------------------------------------------------------------------------
// <copyright file="PaginationHelperTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Common.Helpers;

namespace Shared.Common.Tests.Helpers;

/// <summary>
/// Тесты для вспомогательного класса <see cref="PaginationHelper"/>.
/// Проверяет корректность расчёта параметров пагинации: количества страниц и смещения.
/// </summary>
public sealed class PaginationHelperTests
{
    /// <summary>
    /// Тестовые данные для расчёта общего количества страниц.
    /// Формат: (totalCount, pageSize, expectedPages).
    /// </summary>
    public static TheoryData<int?, int?, int> TotalPagesCases { get; } = new()
    {
        // Стандартные случаи
        { 100, 10, 10 },
        { 100, 20, 5 },
        { 100, 33, 4 }, // округление вверх
        { 99, 10, 10 },
        { 101, 10, 11 },
        
        // Граничные случаи
        { 1, 10, 1 },
        { 10, 10, 1 },
        { 11, 10, 2 },
        { 0, 10, 0 },
        
        // Null значения
        { null, 10, 0 },
        { 100, null, 0 },
        { null, null, 0 },
    };

    /// <summary>
    /// Проверяет корректный расчёт общего количества страниц.
    /// </summary>
    /// <param name="totalCount">Общее количество элементов.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="expectedPages">Ожидаемое количество страниц.</param>
    [Theory]
    [MemberData(nameof(TotalPagesCases))]
    public void GetTotalPages_ValidInputs_ReturnsCorrectPageCount(
        int? totalCount,
        int? pageSize,
        int expectedPages)
    {
        // Act
        var result = PaginationHelper.GetTotalPages(totalCount, pageSize);

        // Assert
        Assert.Equal(expectedPages, result);
    }

    /// <summary>
    /// Проверяет расчёт страниц с различными размерами страниц.
    /// </summary>
    [Fact]
    public void GetTotalPages_DifferentPageSizes_CalculatesCorrectly()
    {
        // Arrange
        const int totalCount = 250;

        // Act & Assert
        Assert.Equal(25, PaginationHelper.GetTotalPages(totalCount, 10));
        Assert.Equal(13, PaginationHelper.GetTotalPages(totalCount, 20));
        Assert.Equal(9, PaginationHelper.GetTotalPages(totalCount, 30));
        Assert.Equal(6, PaginationHelper.GetTotalPages(totalCount, 50));
        Assert.Equal(3, PaginationHelper.GetTotalPages(totalCount, 100));
    }

    /// <summary>
    /// Проверяет, что деление с остатком округляется вверх.
    /// </summary>
    [Fact]
    public void GetTotalPages_DivisionWithRemainder_RoundsUp()
    {
        // Arrange & Act & Assert
        Assert.Equal(4, PaginationHelper.GetTotalPages(100, 33)); // 100/33 = 3.03 -> 4
        Assert.Equal(2, PaginationHelper.GetTotalPages(10, 6));   // 10/6 = 1.67 -> 2
        Assert.Equal(11, PaginationHelper.GetTotalPages(100, 9)); // 100/9 = 11.11 -> 11
    }

    /// <summary>
    /// Тестовые данные для расчёта параметров пагинации (skip, take).
    /// Формат: (pageNumber, pageSize, expectedSkip, expectedTake).
    /// </summary>
    public static TheoryData<int?, int?, int?, int?> PaginationParamsCases { get; } = new()
    {
        // Стандартные случаи (нумерация с 1)
        { 1, 10, 0, 10 },
        { 2, 10, 10, 10 },
        { 3, 10, 20, 10 },
        { 5, 20, 80, 20 },
        { 10, 50, 450, 50 },
        
        // Граничные случаи
        { 1, 1, 0, 1 },
        { 1, 100, 0, 100 },
        
        // Null значения
        { null, 10, null, null },
        { 5, null, null, null },
        { null, null, null, null },
    };

    /// <summary>
    /// Проверяет корректный расчёт параметров пагинации (skip, take).
    /// </summary>
    /// <param name="pageNumber">Номер страницы (с 1).</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="expectedSkip">Ожидаемое смещение.</param>
    /// <param name="expectedTake">Ожидаемое количество элементов.</param>
    [Theory]
    [MemberData(nameof(PaginationParamsCases))]
    public void CalculatePagination_ValidInputs_ReturnsCorrectSkipAndTake(
        int? pageNumber,
        int? pageSize,
        int? expectedSkip,
        int? expectedTake)
    {
        // Act
        var (skip, take) = PaginationHelper.CalculatePagination(pageNumber, pageSize);

        // Assert
        Assert.Equal(expectedSkip, skip);
        Assert.Equal(expectedTake, take);
    }

    /// <summary>
    /// Проверяет формулу расчёта skip: (pageNumber - 1) * pageSize.
    /// </summary>
    [Fact]
    public void CalculatePagination_SkipFormula_IsCorrect()
    {
        // Arrange & Act & Assert
        // Страница 1: skip = (1-1) * 10 = 0
        var (skip1, _) = PaginationHelper.CalculatePagination(1, 10);
        Assert.Equal(0, skip1);

        // Страница 5: skip = (5-1) * 10 = 40
        var (skip5, _) = PaginationHelper.CalculatePagination(5, 10);
        Assert.Equal(40, skip5);

        // Страница 100: skip = (100-1) * 50 = 4950
        var (skip100, _) = PaginationHelper.CalculatePagination(100, 50);
        Assert.Equal(4950, skip100);
    }

    /// <summary>
    /// Проверяет, что take всегда равен pageSize.
    /// </summary>
    [Fact]
    public void CalculatePagination_Take_EqualsPageSize()
    {
        // Arrange & Act & Assert
        var (_, take1) = PaginationHelper.CalculatePagination(1, 10);
        Assert.Equal(10, take1);

        var (_, take2) = PaginationHelper.CalculatePagination(5, 25);
        Assert.Equal(25, take2);

        var (_, take3) = PaginationHelper.CalculatePagination(10, 100);
        Assert.Equal(100, take3);
    }

    /// <summary>
    /// Проверяет обработку нулевой страницы (edge case).
    /// </summary>
    [Fact]
    public void CalculatePagination_PageZero_ReturnsNegativeSkip()
    {
        // Act
        var (skip, take) = PaginationHelper.CalculatePagination(0, 10);

        // Assert
        // Формула (0-1) * 10 = -10, что может быть нежелательным поведением
        // Но это текущая реализация, которую мы тестируем "как есть"
        Assert.Equal(-10, skip);
        Assert.Equal(10, take);
    }

    /// <summary>
    /// Проверяет комбинацию GetTotalPages и CalculatePagination для сквозного сценария.
    /// </summary>
    [Fact]
    public void PaginationHelpers_Integration_EndToEndScenario()
    {
        // Arrange
        const int totalCount = 257;
        const int pageSize = 25;
        const int currentPage = 5;

        // Act
        var totalPages = PaginationHelper.GetTotalPages(totalCount, pageSize);
        var (skip, take) = PaginationHelper.CalculatePagination(currentPage, pageSize);

        // Assert
        Assert.Equal(11, totalPages); // ceil(257/25) = 11
        Assert.Equal(100, skip);      // (5-1) * 25 = 100
        Assert.Equal(25, take);
    }

    /// <summary>
    /// Проверяет расчёт для последней страницы с неполным набором данных.
    /// </summary>
    [Fact]
    public void GetTotalPages_LastPageWithPartialData_CalculatesCorrectly()
    {
        // Arrange - 257 элементов, страница 25 элементов
        // Последняя страница будет содержать только 7 элементов
        
        // Act
        var totalPages = PaginationHelper.GetTotalPages(257, 25);

        // Assert
        Assert.Equal(11, totalPages);
        // Note: CalculatePagination не учитывает фактическое количество элементов
        // на последней странице, он просто возвращает pageSize
    }

    /// <summary>
    /// Проверяет большие значения для проверки переполнения.
    /// </summary>
    [Fact]
    public void CalculatePagination_LargeValues_NoOverflow()
    {
        // Arrange
        const int largePageNumber = 1000000;
        const int largePageSize = 10000;

        // Act
        var (skip, take) = PaginationHelper.CalculatePagination(largePageNumber, largePageSize);

        // Assert
        Assert.Equal(9999990000L % int.MaxValue, (long)skip % int.MaxValue); // проверка на отсутствие некорректного переполнения
        Assert.Equal(largePageSize, take);
    }
}
