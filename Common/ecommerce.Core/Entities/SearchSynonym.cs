using ecommerce.Core.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace ecommerce.Core.Entities
{
    public class SearchSynonym : AuditableEntity<int>
    {
        [Required]
        [MaxLength(250)]
        public string Keyword { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Synonyms { get; set; } = string.Empty; // Comma-separated list of synonyms
        
        public bool IsBidirectional { get; set; } = true;

        public SearchSynonymCategory Category { get; set; } = SearchSynonymCategory.General;
    }

    public enum SearchSynonymCategory
    {
        [Display(Name = "Genel Eş Anlamlı")]
        General = 0,
        [Display(Name = "Roma Rakamı")]
        RomanNumeral = 1,
        [Display(Name = "Teknik Terim (V-Belt vb.)")]
        TechnicalVTerm = 2
    }
}
