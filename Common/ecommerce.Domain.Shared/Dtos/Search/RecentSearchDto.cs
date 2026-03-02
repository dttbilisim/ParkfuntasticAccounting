namespace ecommerce.Domain.Shared.Dtos.Search
{
    public class RecentSearchDto
    {
        public string Term { get; set; } = string.Empty;
        public DateTime SearchDate { get; set; }
        public int RecordCount { get; set; }
    }
}
