namespace ecommerce.Core.Models
{
    public sealed class PageSetting
    {
        public PageSetting()
        {
        }

        public PageSetting(string? filter, string? orderBy, int? skip = null, int? take = null, bool export = false)
        {
            Filter = filter;
            OrderBy = orderBy;
            
            Skip = skip;
            Take = take;
            Export = export;
        }

        public string? Filter { get; set; }
        public string? Search { get; set; }
        public string? OrderBy { get; set; }
        public string? OrderByDir{get;set;}
        public int? Skip { get; set; }
        public int? Take { get; set; }
        public bool Export { get; set; }
    }
}