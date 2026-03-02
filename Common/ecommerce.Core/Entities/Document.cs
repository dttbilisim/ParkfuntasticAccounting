using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class DocumentFile: AuditableEntity<int>{

    public string Name{get;set;}
}
