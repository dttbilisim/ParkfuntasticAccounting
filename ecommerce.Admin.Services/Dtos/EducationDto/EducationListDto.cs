using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.EducationDto;

[AutoMap(typeof(Education))]
public class EducationListDto{
    public int? Id{get;set;}
    public EducationCategoryType EducationCategoryType { get; set; }

    public int Status{get;set;}
    public string Name{get;set;}
    public string SubText{get;set;}
    public string Description{get;set;}
    public int CategoryId{get;set;}
    public string PubliserName{get;set;}
    public string ImageUrl{get;set;}
    public string ButtonText{get;set;}
    public string ButtonUrl{get;set;}
    public int Order {get;set;}
    public DateTime StartDate{get;set;}
    public DateTime EndDate{get;set;}
    public DateTime CreatedDate{get;set;}
    
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
