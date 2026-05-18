using System.Text;
using Shared.Presentation.Core.Exceptions;

namespace Shared.Presentation.Core.Tests.Exceptions;

/// <summary>
/// Тесты для <see cref="Shared.Presentation.Core.Exceptions.StringBuilderPolicy"/> — проверка создания, очистки и возврата <see cref="StringBuilder"/>.
/// </summary>
public sealed class StringBuilderPolicyTests
{
    /// <summary>
    /// Проверяет, что метод <see cref="Shared.Presentation.Core.Exceptions.StringBuilderPolicy.Create"/> возвращает <see cref="StringBuilder"/>
    /// с capacity не менее 1024.
    /// </summary>
    [Fact]
    public void Create_ReturnsStringBuilderWithCapacityAtLeast1024()
    {
        // Arrange
        var policy = new StringBuilderPolicy();

        // Act
        var sb = policy.Create();

        // Assert
        sb.Capacity.Should().BeGreaterOrEqualTo(1024);
    }

    /// <summary>
    /// Проверяет, что метод <see cref="Shared.Presentation.Core.Exceptions.StringBuilderPolicy.Return"/> очищает <see cref="StringBuilder"/>
    /// (длина становится 0) и возвращает <c>true</c>.
    /// </summary>
    [Fact]
    public void Return_ClearsStringBuilder()
    {
        // Arrange
        var policy = new StringBuilderPolicy();
        var sb = new StringBuilder();
        sb.Append("abc");

        // Act
        var result = policy.Return(sb);

        // Assert
        sb.Length.Should().Be(0);
        result.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что метод <see cref="Shared.Presentation.Core.Exceptions.StringBuilderPolicy.Return"/> всегда возвращает <c>true</c>.
    /// </summary>
    [Fact]
    public void Return_AlwaysReturnsTrue()
    {
        // Arrange
        var policy = new StringBuilderPolicy();

        // Act
        var result = policy.Return(new StringBuilder());

        // Assert
        result.Should().BeTrue();
    }
}
