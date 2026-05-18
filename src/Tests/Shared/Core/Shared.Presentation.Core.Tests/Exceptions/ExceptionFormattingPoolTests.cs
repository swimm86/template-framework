using System.Text;
using Microsoft.Extensions.ObjectPool;
using Shared.Presentation.Core.Exceptions;

namespace Shared.Presentation.Core.Tests.Exceptions;

/// <summary>
/// Тесты для <see cref="Shared.Presentation.Core.Exceptions.ExceptionFormattingPool"/> — проверка инициализации и переиспользования пула <see cref="StringBuilder"/>.
/// </summary>
/// <remarks>
/// Используется локальный экземпляр <c>DefaultObjectPool&lt;StringBuilder&gt;</c>
/// вместо глобального <see cref="Shared.Presentation.Core.Exceptions.ExceptionFormattingPool.StringBuilder"/>,
/// чтобы избежать гонок при параллельном выполнении тестов мапперов.
/// </remarks>
public sealed class ExceptionFormattingPoolTests
{
    /// <summary>
    /// Проверяет, что после возврата экземпляра <see cref="StringBuilder"/> в пул,
    /// при повторном получении возвращается объект с очищенным содержимым и достаточной ёмкостью.
    /// </summary>
    [Fact]
    public void StringBuilder_ReturnThenGet_ReturnsCleanInstance()
    {
        // Arrange
        var pool = new DefaultObjectPool<StringBuilder>(new StringBuilderPolicy());
        var instance = pool.Get();
        instance.Append("test data");

        // Act
        pool.Return(instance);
        var reused = pool.Get();

        // Assert
        reused.Length.Should().Be(0);
        reused.Capacity.Should().BeGreaterOrEqualTo(1024);
    }
}
