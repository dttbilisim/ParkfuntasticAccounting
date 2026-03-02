using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.EducationDto;

[AutoMap(typeof(EducationCategory), ReverseMap = true)]
public class EducationCategoryUpsertDto{
    public int? Id{get;set;}
    public int Status{get;set;}
    public string ? Name { get; set; } = null!;
    public int? ParentId { get; set; }
    public int Order { get; set; }
    public EducationCategoryType EducationCategoryType { get; set; }

    [Ignore]
    public bool StatusBool { get; set; }
}
