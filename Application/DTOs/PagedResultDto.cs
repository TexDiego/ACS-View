namespace ACS_View.Application.DTOs
{
    public sealed class PagedResultDto<T>
    {
        public IReadOnlyList<T> Items { get; init; } = [];
        public int TotalCount { get; init; }
    }
}
