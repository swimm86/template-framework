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

        // pageSize == 0: защита от деления на ноль
        { 100, 0, 0 },
        { 1, 0, 0 },
        { 0, 0, 0 },

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
        result.Should().Be(expectedPages);
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
        PaginationHelper.GetTotalPages(totalCount, 10).Should().Be(25);
        PaginationHelper.GetTotalPages(totalCount, 20).Should().Be(13);
        PaginationHelper.GetTotalPages(totalCount, 30).Should().Be(9);
        PaginationHelper.GetTotalPages(totalCount, 50).Should().Be(5);
        PaginationHelper.GetTotalPages(totalCount, 100).Should().Be(3);
    }

    /// <summary>
    /// Проверяет, что деление с остатком округляется вверх.
    /// </summary>
    [Fact]
    public void GetTotalPages_DivisionWithRemainder_RoundsUp()
    {
        // Arrange & Act & Assert
        PaginationHelper.GetTotalPages(100, 33).Should().Be(4); // 100/33 = 3.03 -> 4
        PaginationHelper.GetTotalPages(10, 6).Should().Be(2);   // 10/6 = 1.67 -> 2
        PaginationHelper.GetTotalPages(100, 9).Should().Be(12); // 100/9 = 11.11 -> 12
    }

    /// <summary>
    /// Тестовые данные для невалидных (отрицательных) входов <see cref="PaginationHelper.GetTotalPages"/>.
    /// Формат: (totalCount, pageSize).
    /// </summary>
    public static TheoryData<int, int> NegativeTotalPagesInputsCases { get; } = new()
    {
        // Отрицательный pageSize
        { 100, -10 },
        { 0, -1 },

        // Отрицательный totalCount
        { -100, 10 },
        { -1, 5 },

        // Оба отрицательны
        { -1, -1 },
        { -100, -10 },
    };

    /// <summary>
    /// Проверяет, что <see cref="PaginationHelper.GetTotalPages"/> бросает
    /// <see cref="ArgumentOutOfRangeException"/> при отрицательных входах
    /// (симметрично с <see cref="PaginationHelper.CalculatePagination"/>).
    /// </summary>
    /// <param name="totalCount">Общее количество элементов.</param>
    /// <param name="pageSize">Размер страницы.</param>
    [Theory]
    [MemberData(nameof(NegativeTotalPagesInputsCases))]
    public void GetTotalPages_NegativeInputs_ThrowsArgumentOutOfRangeException(
        int totalCount,
        int pageSize)
    {
        // Act
        Action act = () => PaginationHelper.GetTotalPages(totalCount, pageSize);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .Where(ex => ex.ParamName == nameof(totalCount) || ex.ParamName == nameof(pageSize));
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
        skip.Should().Be(expectedSkip);
        take.Should().Be(expectedTake);
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
        skip1.Should().Be(0);

        // Страница 5: skip = (5-1) * 10 = 40
        var (skip5, _) = PaginationHelper.CalculatePagination(5, 10);
        skip5.Should().Be(40);

        // Страница 100: skip = (100-1) * 50 = 4950
        var (skip100, _) = PaginationHelper.CalculatePagination(100, 50);
        skip100.Should().Be(4950);
    }

    /// <summary>
    /// Проверяет, что take всегда равен pageSize.
    /// </summary>
    [Fact]
    public void CalculatePagination_Take_EqualsPageSize()
    {
        // Arrange & Act & Assert
        var (_, take1) = PaginationHelper.CalculatePagination(1, 10);
        take1.Should().Be(10);

        var (_, take2) = PaginationHelper.CalculatePagination(5, 25);
        take2.Should().Be(25);

        var (_, take3) = PaginationHelper.CalculatePagination(10, 100);
        take3.Should().Be(100);
    }

    /// <summary>
    /// Тестовые данные для невалидных (неположительных) входов <see cref="PaginationHelper.CalculatePagination"/>.
    /// Формат: (pageNumber, pageSize).
    /// </summary>
    public static TheoryData<int, int> InvalidPaginationInputsCases { get; } = new()
    {
        // pageNumber вне допустимого диапазона (нумерация с 1)
        { 0, 10 },
        { -1, 10 },
        { -100, 10 },

        // pageSize вне допустимого диапазона (должен быть > 0)
        { 1, 0 },
        { 1, -10 },

        // Оба параметра невалидны
        { 0, 0 },
        { -5, -5 },
    };

    /// <summary>
    /// Проверяет, что метод бросает <see cref="ArgumentOutOfRangeException"/>
    /// при неположительных значениях <paramref name="pageNumber"/> или <paramref name="pageSize"/>,
    /// причём <see cref="ArgumentException.ParamName"/> указывает на конкретный невалидный параметр.
    /// </summary>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    [Theory]
    [MemberData(nameof(InvalidPaginationInputsCases))]
    public void CalculatePagination_NonPositiveInputs_ThrowsArgumentOutOfRangeException(
        int pageNumber,
        int pageSize)
    {
        // Act
        Action act = () => PaginationHelper.CalculatePagination(pageNumber, pageSize);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .Where(ex => ex.ParamName == nameof(pageNumber) || ex.ParamName == nameof(pageSize));
    }

    /// <summary>
    /// Проверяет, что при <c>null</c>-входах guard срабатывает раньше валидации диапазона:
    /// возвращается <c>(null, null)</c>, исключение не бросается.
    /// </summary>
    /// <param name="pageNumber">Номер страницы (может быть null).</param>
    /// <param name="pageSize">Размер страницы (может быть null).</param>
    [Theory]
    [InlineData(null, null)]
    [InlineData(null, 0)]
    [InlineData(null, -1)]
    [InlineData(0, null)]
    [InlineData(-1, null)]
    public void CalculatePagination_NullInput_ReturnsNullsAndDoesNotThrow(
        int? pageNumber,
        int? pageSize)
    {
        // Act
        var (skip, take) = PaginationHelper.CalculatePagination(pageNumber, pageSize);

        // Assert
        skip.Should().BeNull();
        take.Should().BeNull();
    }

    /// <summary>
    /// Проверяет согласованную работу <see cref="PaginationHelper.GetTotalPages"/>
    /// и <see cref="PaginationHelper.CalculatePagination"/> на одном наборе входных данных.
    /// </summary>
    [Fact]
    public void PaginationHelper_GetTotalPagesAndCalculatePagination_ProducesConsistentResults()
    {
        // Arrange
        const int totalCount = 257;
        const int pageSize = 25;
        const int currentPage = 5;

        // Act
        var totalPages = PaginationHelper.GetTotalPages(totalCount, pageSize);
        var (skip, take) = PaginationHelper.CalculatePagination(currentPage, pageSize);

        // Assert
        totalPages.Should().Be(11); // ceil(257/25) = 11
        skip.Should().Be(100);      // (5-1) * 25 = 100
        take.Should().Be(25);
    }

    /// <summary>
    /// Проверяет расчёт для последней страницы с неполным набором данных.
    /// </summary>
    [Fact]
    public void GetTotalPages_LastPageWithPartialData_CalculatesCorrectly()
    {
        // Arrange - 257 элементов, страница 25 элементов;
        // последняя страница будет содержать только 7 элементов.

        // Act
        var totalPages = PaginationHelper.GetTotalPages(257, 25);

        // Assert
        totalPages.Should().Be(11);

        // Note: CalculatePagination не учитывает фактическое количество элементов
        // на последней странице, он просто возвращает pageSize.
    }
}
