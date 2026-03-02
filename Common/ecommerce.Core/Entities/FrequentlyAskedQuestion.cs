using ecommerce.Core.Entities.Base;
using ecommerce.Core.Utils;

namespace ecommerce.Core.Entities;

public class FrequentlyAskedQuestion : AuditableEntity<int>
{
    public SSSAndBlogGroup Group { get; set; }
    public int? ParentId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int? Order{get;set;}
}