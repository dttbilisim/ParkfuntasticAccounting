using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
namespace ecommerce.Admin.Domain.Dtos.EducationDto;

[AutoMap(typeof(EducationImages))]
public class EducationImagesListDto{
    public int Id{get;set;}
    public int Status{get;set;}
    public int EducationItemId{get;set;}
    public string ItemUrl{get;set;}
    public int Order {get;set;}
    
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
