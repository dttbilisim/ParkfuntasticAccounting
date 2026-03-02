using ecommerce.Core.Interfaces;
namespace ecommerce.Core.Entities;
public class ProductOnline:IEntity<int>{
    public int Id{get;set;}
    public int ProductId{get;set;}
    public string ? Name{get;set;}
    public string ? ImageUrl{get;set;}
    public string ?Barcodes{get;set;}
    public string ? Brand{get;set;}
    public decimal ? Psf{get;set;} = 0;
    public decimal ? MinPrice{get;set;} = 0;
    public decimal ? MaxPrice{get;set;} = 0;
    public DateTime Created{get;set;}=DateTime.Now;
    public DateTime? Updated{get;set;}
    public string ? ImageUrlNew {get;set;}

}

