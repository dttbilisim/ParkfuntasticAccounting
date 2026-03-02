using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;
using ecommerce.Core.Utils;
namespace ecommerce.Core.Entities;
public class OrderInvoice: AuditableEntity<int>{

    public InvoiceType InvoiceType{get;set;}

    public DateTime InvoiceDate{get;set;}
    public int OrderId{get;set;}
    public int CompanyId{get;set;}
    public string InvoicePath{get;set;}
    public string FileName{get;set;}
    
    [ForeignKey("CompanyId")] public Company Company{get;set;}
    [ForeignKey("OrderId")] public Orders Orders{get;set;}
    
    
}
