namespace Shared.Presentation.Core.Tests.Infrastructure;

/// <summary>
/// Генерация исключений с контролируемой глубиной и реальным StackTrace.
/// </summary>
internal static class ExceptionFactory
{
    /// <summary>
    /// Строит цепочку Inner exception заданной глубины (включая корень).
    /// </summary>
    /// <param name="depth">Количество уровней (≥ 1).</param>
    public static Exception WithInnerDepth(int depth)
    {
        if (depth < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(depth));
        }

        Exception ex = new InvalidOperationException("level-0");
        for (var i = 1; i < depth; i++)
        {
            ex = new InvalidOperationException($"level-{i}", ex);
        }

        return ex;
    }

    /// <summary>
    /// Бросает и ловит исключение, чтобы оно получило реальный StackTrace.
    /// </summary>
    public static T Thrown<T>(T exception)
        where T : Exception
    {
        try
        {
            throw exception;
        }
        catch (T caught)
        {
            return caught;
        }
    }
}
