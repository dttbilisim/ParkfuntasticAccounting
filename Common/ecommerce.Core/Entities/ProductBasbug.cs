using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class ProductBasbug:AuditableEntity<int>{
    public string No { get; set; }
    public string Aciklama1 { get; set; }
    public string ? Aciklama2 { get; set; }
    public string ? MarkaKod { get; set; }
    public string ? OemKod { get; set; }
    public string ? Uretici { get; set; }
    public string ? GrupKod { get; set; }
    public string ? Model { get; set; }
    public string Motor { get; set; }
    public string ? Yil { get; set; }
    public string ? Birim { get; set; }
    public string ? ParaBirimi { get; set; }
    public decimal Fiyat { get; set; }
    public int? Stok{get;set;}
}
