using AutoMapper;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
namespace ecommerce.Admin.Domain.Dtos.ProductDto;
[AutoMap(typeof(ProductTransaction))]
public class ProductTransactionDto{
    public int? Id { get; set; }
    public DateTime CreatedDate { get; set; }
    public int CreatedId{get;set;}
    public EntityStatus Status { get; set; }
    
    public string Product{get;set;}
    public string Category{get;set;}
    public string Manufacturer{get;set;}
    public string GroupCode{get;set;}
    public string Barcode{get;set;}
    public string Form{get;set;}
    public string ActiveSubstanceName{get;set;}
    public string ActiveSubstanceNumberofForms{get;set;}
    public decimal ActiveIngredientAmount{get;set;}
    public string SubstanceQuantityType{get;set;}
    public decimal Length{get;set;}
    public decimal Height{get;set;}
    public decimal Width{get;set;}
    public decimal ReatilPrice{get;set;}
}
