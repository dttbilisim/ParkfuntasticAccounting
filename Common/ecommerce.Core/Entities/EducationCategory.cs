using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;
using ecommerce.Core.Utils;

namespace ecommerce.Core.Entities;
public class EducationCategory:AuditableEntity<int>{
   
        public string ? Name { get; set; } = null!;
        public int? ParentId { get; set; }
        public int Order { get; set; }
        public EducationCategoryType EducationCategoryType { get; set; }

        
        [ForeignKey("ParentId")]
        public EducationCategory? Parent { get; set; }      
    
}
