using Shared.Presentation.Core.Exceptions.Settings;

namespace Shared.Presentation.Core.Tests.Exceptions.Settings;

/// <summary>
/// Тесты для <see cref="ExceptionMapperSettings"/> — проверка значений по умолчанию, явных значений и record-равенства.
/// </summary>
public sealed class ExceptionMapperSettingsTests
{
    /// <summary>
    /// Проверяет, что конструктор без параметров устанавливает документированные значения по умолчанию:
    /// <see cref="ExceptionMapperSettings.ShouldEnrichWithTrace"/> = true,
    /// <see cref="ExceptionMapperSettings.StackTraceDepth"/> = 10,
    /// <see cref="ExceptionMapperSettings.MaxExceptionDepth"/> = 5.
    /// </summary>
    [Fact]
    public void DefaultConstructor_AppliesDocumentedDefaults()
    {
        // Arrange

        // Act
        var settings = new ExceptionMapperSettings();

        // Assert
        settings.ShouldEnrichWithTrace.Should().BeTrue();
        settings.StackTraceDepth.Should().Be(10);
        settings.MaxExceptionDepth.Should().Be(5);
    }

    /// <summary>
    /// Проверяет, что конструктор с явными значениями корректно сохраняет переданные параметры.
    /// </summary>
    [Fact]
    public void Constructor_WithExplicitValues_StoresThem()
    {
        // Arrange

        // Act
        var settings = new ExceptionMapperSettings(
            ShouldEnrichWithTrace: false,
            StackTraceDepth: 3,
            MaxExceptionDepth: 2);

        // Assert
        settings.ShouldEnrichWithTrace.Should().BeFalse();
        settings.StackTraceDepth.Should().Be(3);
        settings.MaxExceptionDepth.Should().Be(2);
    }

    /// <summary>
    /// Проверяет, что два экземпляра <see cref="ExceptionMapperSettings"/> с одинаковыми значениями
    /// считаются равными через <c>==</c> и <see cref="object.Equals(object?)"/>.
    /// </summary>
    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        // Arrange
        var a = new ExceptionMapperSettings(false, 3, 2);
        var b = new ExceptionMapperSettings(false, 3, 2);

        // Act
        var equals = a == b;

        // Assert
        a.Should().Be(b);
        equals.Should().BeTrue();
    }
}
