using ecommerce.Core.Entities.Base;
using ecommerce.Core.Utils;
namespace ecommerce.Core.Entities;
public class EmailTemplates: AuditableEntity<int>{

    public string Name{get;set;}
    public string Description{get;set;}
    public EmailTemplateType EmailTemplateType{get;set;}
    /// <summary>Şube (tenant) - null ise tüm şubeler için paylaşımlı kabul edilebilir.</summary>
    public int? BranchId { get; set; }
}
