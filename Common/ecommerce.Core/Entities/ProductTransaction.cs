using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class ProductTransaction : AuditableEntity<int>{
    public string Product{get;set;}
    public string ? Category{get;set;}
    public string ? SubCategory1{get;set;}
    public string ? SubCategory2{get;set;}
    public string ? Manufacturer{get;set;}
    public string Barcode{get;set;}
    public string ? Form{get;set;}
    public int ? Tax{get;set;} = 0;
    public decimal ? Length{get;set;} = 0;
    public decimal ? Height{get;set;} = 0;
    public decimal ? Width{get;set;} = 0;
    public decimal ? Weight{get;set;} = 0;
    public decimal ? Desi{get;set;} = 0;
    public decimal ReatilPrice{get;set;} = 0;
    public string ? ProductCode{get;set;}
    public string ? ProductId{get;set;}
    public string ? ImageUrl{get;set;}
    public int? CompanyId{get;set;}
    

}
