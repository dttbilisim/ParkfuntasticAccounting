using AutoMapper;
using ecommerce.Core.Entities;

namespace ecommerce.Admin.Domain.Dtos.SearchSynonymDto
{
    [AutoMap(typeof(SearchSynonym))]
    public class SearchSynonymListDto
    {
        public int Id { get; set; }
        public string Keyword { get; set; } = string.Empty;
        public string Synonyms { get; set; } = string.Empty;
        public bool IsBidirectional { get; set; }
        public int Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public SearchSynonymCategory Category { get; set; }
    }
}
