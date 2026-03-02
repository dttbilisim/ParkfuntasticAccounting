using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities
{
	public class Category:AuditableEntity<int>
	{
        public string Name { get; set; } = null!;
        public int? ParentId { get; set; }
        public int BranchId { get; set; }

        public bool IsMainPage { get; set; }
        public bool IsMainSlider{get;set;}
        public int ? SubCategoryCount{get;set;} = 0;
        public int ? Height{get;set;} = 0;
        public int Order { get; set; }
        public List<ProductCategories> Products { get; set; } = null!;
        [ForeignKey("ParentId")]
        public Category? Parent { get; set; }
        public ICollection<Category> SubCategories { get; set; } = new List<Category>();
    
        public string ? ImageUrl{get;set;}
        
    }
}

