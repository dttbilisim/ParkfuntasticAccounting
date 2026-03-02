using AutoMapper;
using ecommerce.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace ecommerce.Admin.Domain.Dtos.SearchSynonymDto
{
    [AutoMap(typeof(SearchSynonym), ReverseMap = true)]
    public class SearchSynonymUpsertDto
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Anahtar kelime gereklidir")]
        [MaxLength(250)]
        public string Keyword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Eş anlamlılar gereklidir")]
        [MaxLength(500)]
        public string Synonyms { get; set; } = string.Empty;

        public bool IsBidirectional { get; set; } = true;
        
        public SearchSynonymCategory Category { get; set; } = SearchSynonymCategory.General;
        
        public int Status { get; set; }
        
        public bool StatusBool { get; set; }
    }
}
