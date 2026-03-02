using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities
{
    public class ActiveArticle : AuditableEntity<int>
    {
        public string Name { get; set; } = null!;
    }
}
