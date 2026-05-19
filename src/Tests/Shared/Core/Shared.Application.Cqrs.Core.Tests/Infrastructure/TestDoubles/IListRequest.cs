namespace Shared.Application.Cqrs.Core.Tests.Infrastructure.TestDoubles;

public interface IListRequest<TFilter>
    where TFilter : new()
{
    TFilter? Filter { get; }
    int PageNumber { get; }
    int PageSize { get; }
    ICollection<string>? SortOptions { get; }
}
