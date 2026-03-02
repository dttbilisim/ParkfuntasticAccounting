using AutoMapper;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
namespace ecommerce.Admin.Domain.Dtos.CompanyDto;
[AutoMap(typeof(PharmacyData))]

public class PharmacyDataDto{
    public int Id { get; set; }
    public string ? PharmacyType{get;set;}
    public string ? GlnNumber{get;set;}
    public string ? PharmacyName{get;set;}
    public string ? Email{get;set;}
    public string ? StatusText{get;set;}
    public EntityStatus Status { get; set; }
    public string ? City{get;set;}
    public string ? Town{get;set;}
}
