namespace ecommerce.Core.Models
{
    public class Paging<T>
    {
        public T Data { get; set; }
        public int DataCount { get; set; }
        public int TotalRawCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }
}
