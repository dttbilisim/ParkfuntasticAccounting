using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
namespace ecommerce.Admin.Domain.Dtos.CompanyDto;
[AutoMap(typeof(CompanyWareHouse))]
public class CompanyWarehouseListDto{
    public int Id{get;set;}
   
    public int CompanyId{get;set;}
    public int CityId{get;set;}
    public int TownId{get;set;}
    public EntityStatus Status{get;set;}
    [Ignore]
    public string StatusStr{
        get{
            switch((int) Status){
                case 0:
                    return "Pasif";
                case 1:
                    return "Aktif";
                case 99:
                    return "Silinmi?";
                default:
                    return "Belirsiz";
            }
            ;
        }
    }
    public City City{get;set;}
    public Town Town{get;set;}
}
