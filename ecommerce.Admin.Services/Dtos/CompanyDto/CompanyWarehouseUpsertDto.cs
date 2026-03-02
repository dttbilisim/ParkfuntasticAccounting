using AutoMapper;
using ecommerce.Core.Entities;
namespace ecommerce.Admin.Domain.Dtos.CompanyDto;
[AutoMap(typeof(CompanyWareHouse), ReverseMap = true)]
public class CompanyWarehouseUpsertDto{

    public int Id{get;set;}
    public int CompanyId{get;set;}
    public int CityId{get;set;}
    public int TownId{get;set;}
    public DateTime CreatedDate { get; set; }
    public int CreatedId { get; set; }
    

}
