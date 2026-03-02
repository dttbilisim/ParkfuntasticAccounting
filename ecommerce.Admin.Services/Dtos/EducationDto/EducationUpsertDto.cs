using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.EducationDto;

[AutoMap(typeof(Education), ReverseMap = true)]
public class EducationUpsertDto{
    
    public int? Id{get;set;}
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
    [Ignore]
    public bool StatusBool { get; set; }

    public EducationCategoryType EducationCategoryType { get; set; }
    public bool IsSlider { get; set; }

}
