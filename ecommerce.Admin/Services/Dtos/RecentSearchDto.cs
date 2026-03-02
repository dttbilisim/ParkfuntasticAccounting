namespace ecommerce.Admin.Services.Dtos
{
    public class RecentSearchDto
    {
        public string Term { get; set; } = string.Empty;
        public DateTime SearchDate { get; set; }
        public int RecordCount { get; set; }
    }
}
