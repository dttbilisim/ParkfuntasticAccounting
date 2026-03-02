using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class ProductGroupCode: AuditableEntity<int>{
   
    public int ProductId {get;set;}
    public string OemCode {get;set;} = null!; // Renamed from GroupCode
    public string SourceType {get;set;} = "OEM"; //Saticinin tablosundaki Id olacak
    
    
    [ForeignKey("ProductId")]
    [JsonIgnore]
    public Product Product { get; set; } = null!;
}
