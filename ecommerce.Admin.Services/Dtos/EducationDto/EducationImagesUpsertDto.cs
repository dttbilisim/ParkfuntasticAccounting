using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
namespace ecommerce.Admin.Domain.Dtos.EducationDto;

[AutoMap(typeof(EducationImages), ReverseMap = true)]
public class EducationImagesUpsertDto{
    public int? Id{get;set;}
    public int Status{get;set;}
    public int EducationItemId{get;set;}
    public string ItemUrl{get;set;}
    public int Order {get;set;}
    [Ignore]
    public bool StatusBool { get; set; }
}
