namespace ProyectoFarmaVita.Models
{
    public class MPaginatedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNextPage => PageNumber < TotalPages;
        public bool HasPreviousPage => PageNumber > 1;
        public int StartItem => (PageNumber - 1) * PageSize + 1;
        public int EndItem => Math.Min(PageNumber * PageSize, TotalCount);
    }
}