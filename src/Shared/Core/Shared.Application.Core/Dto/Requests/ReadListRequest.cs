//namespace Shared.Application.Core.Dto.Requests;

///// <summary>
///// Бзаовый класс для чтения с пагинацией
///// </summary>
///// <typeparam name="TResponse">Ожидаемый ответ</typeparam>
///// <typeparam name="TFilter">Фильтр</typeparam>
//public abstract class ReadListRequest<TFilter, TResponse>(
//    int? pageNumber = default,
//    int? pageSize = default,
//    TFilter? filter = default,
//    List<string>? sortOptions = default)
//    : IRequest<TResponse>
//    where TFilter : new()
//{
//    /// <summary>
//    /// Разделитель значений в объекте строки.
//    /// </summary>
//    private const char ValueDelimiter = '.';

//    /// <summary>
//    /// Минимальный номер страницы.
//    /// </summary>
//    private const int MinPageNumber = 1;

//    /// <summary>
//    /// Номер страницы.
//    /// </summary>
//    public int PageNumber { get; } = pageNumber is null or < MinPageNumber ? MinPageNumber : pageNumber.Value;

//    /// <summary>
//    /// Количество элементов на одной странице.
//    /// </summary>
//    public int? PageSize { get; } = pageSize;

//    /// <summary>
//    /// Фильтр.
//    /// </summary>
//    public TFilter Filter { get; } = filter ?? new TFilter();

//    /// <summary>
//    /// Параметры сортировки.
//    /// </summary>
//    public List<SortOption> SortOptions { get; } = ConvertSortOptions(sortOptions);

//    private static List<SortOption> ConvertSortOptions(List<string>? sortOptions)
//    {
//        if (sortOptions is null) return [];

//        return sortOptions
//            .Where(value => !string.IsNullOrEmpty(value))
//            .Select((value, index) =>
//            {
//                var sortOptionValues = value.Split(ValueDelimiter);
//                return new SortOption(
//                    key: sortOptionValues[0],
//                    order: index,
//                    directionType: GetDirectionType(sortOptionValues.ElementAtOrDefault(1)));
//            })
//            .ToList();
//    }

//    private static OrderDirectionType GetDirectionType(string? str) =>
//        str?.ToLower() switch
//        {
//            "desc" => OrderDirectionType.Descending,
//            _ => OrderDirectionType.Ascending
//        };
//}
