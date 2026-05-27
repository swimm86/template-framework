using Shared.Common.Batch;

namespace Shared.Common.Tests;

/// <summary>
/// Тесты <see cref="BatchHelper"/>: потоковая выборка батчей и обработка с признаком «пустой батч».
/// </summary>
public sealed class BatchHelperTests
{
    /// <summary>
    /// Исходный массив, размер батча и ожидаемые непустые чанки для потоковой перегрузки <see cref="BatchHelper.BatchSelectAsync{TBatch}(Func{int,int,Task{TBatch}},Func{TBatch,bool},Func{TBatch,int},int,Func{bool},CancellationToken)"/>.
    /// </summary>
    public static TheoryData<int[], int, int[][]> StreamBatchCases { get; } = new()
    {
        {
            [1, 2, 3, 4, 5],
            2,
            [[1, 2], [3, 4], [5]]
        },
        {
            [1, 2, 3],
            2,
            [[1, 2], [3]]
        },
    };

    /// <summary>
    /// При предикате «батч пуст» обработчик не вызывается для пустых срезов; непустые батчи обрабатываются с ожидаемыми размерами.
    /// </summary>
    [Fact]
    public async Task ProcessBatchesAsync_WhenBatchEmptyByPredicate_SkipsEmpty_DoesNotInvokeHandler()
    {
        // Arrange
        var processedBatches = new List<IReadOnlyCollection<int>>();
        var source = Enumerable.Range(1, 3).ToArray();

        // Act
        await BatchHelper.ProcessBatchesAsync(
            getBatchFunc: BuildBatchFunc(source),
            isBatchEmptyFunc: batch => batch.Count == 0,
            batchSizeFunc: batch => batch.Count,
            processBatchAction: batch =>
            {
                processedBatches.Add(batch);
                return Task.CompletedTask;
            },
            batchSize: 2,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        processedBatches.Select(batch => batch.Count)
            .Should().Equal(2, 1);
    }

    /// <summary>
    /// Перегрузка <see cref="BatchHelper.BatchSelectAsync{TObject}(Func{int,int,Task{ICollection{TObject}}},int,CancellationToken)"/> режет источник на чанки заданного размера; последний чанк может быть короче.
    /// </summary>
    /// <param name="source">Все элементы выборки.</param>
    /// <param name="batchSize">Размер страницы.</param>
    /// <param name="expectedChunks">Ожидаемые массивы подряд идущих элементов.</param>
    [Theory]
    [MemberData(nameof(StreamBatchCases))]
    public async Task BatchSelectAsync_StreamOverload_ReturnsExpectedNonEmptyChunks(
        int[] source,
        int batchSize,
        int[][] expectedChunks)
    {
        // Arrange
        var processedBatches = new List<int[]>();

        // Act
        await foreach (var batch in BatchHelper.BatchSelectAsync(
                           BuildCollectionBatchFunc(source),
                           batchSize,
                           TestContext.Current.CancellationToken))
        {
            processedBatches.Add(batch.ToArray());
        }

        // Assert
        processedBatches.Should().Equal(expectedChunks, (actual, expected) => actual.SequenceEqual(expected));
    }

    private static Func<int, int, Task<IReadOnlyCollection<int>>> BuildBatchFunc(IReadOnlyCollection<int> source)
    {
        return (skip, take) =>
        {
            var batch = source.Skip(skip).Take(take).ToArray();
            return Task.FromResult<IReadOnlyCollection<int>>(batch);
        };
    }

    private static Func<int, int, Task<ICollection<int>>> BuildCollectionBatchFunc(IReadOnlyCollection<int> source)
    {
        return (skip, take) =>
        {
            var batch = source.Skip(skip).Take(take).ToArray();
            return Task.FromResult<ICollection<int>>(batch);
        };
    }
}
