namespace PerfumeTracker.Server.Helpers;

public class PaginatedResult<TEntity>(int page, int pageSize, long totalCount, IEnumerable<TEntity> items)
	where TEntity : class {
	public int Page { get; } = page;
	public int PageSize { get; } = pageSize;
	public long TotalCount { get; } = totalCount;
	public IEnumerable<TEntity> Items { get; } = items;
}
