using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
namespace ecommerce.Admin.Domain.Dtos.EducationDto;

[AutoMap(typeof(EducationItems))]
public class EducationItemsListDto{

    public int Id{get;set;}
    public int Status{get;set;}
    public int EducationId{get;set;}
    public string Name{get;set;}
    public string SubText{get;set;}
    public string Description{get;set;}
    public int Order {get;set;}
    public DateTime CreatedDate{get;set;}
    
    public string EducationName { get;set;}

    [Ignore]
    public string StatusStr
    {
        get
        {
            switch (Status)
            {
                case 0: return "Pasif";
                case 1: return "Aktif";
                case 99: return "Silinmiş";
                default: return "Belirsiz";
            };
        }
    }

   
}
