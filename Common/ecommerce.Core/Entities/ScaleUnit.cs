using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities
{
    public class ScaleUnit : AuditableEntity<int>
    {
        public string Name { get; set; }
    }
}
