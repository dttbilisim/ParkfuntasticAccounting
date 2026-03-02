using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class ReportStorage: AuditableEntity<int>{
    public string Name{get;set;}
    public string ReportSql{get;set;}
    public string ReportModel{get;set;}
    
}
