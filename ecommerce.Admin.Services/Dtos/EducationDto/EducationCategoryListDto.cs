using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.EducationDto;

[AutoMap(typeof(EducationCategory))]
public class EducationCategoryListDto{
    public int Id { get; set; }
    public string IdStr
    {
        get
        {
            return Id.ToString();
        }
    }

    public EducationCategoryType EducationCategoryType { get; set; }

    public string Name { get; set; } = null!;
    public int? ParentId { get; set; }
    public string? ParentName { get { return Parent?.Name; } }
    public EducationCategoryListDto? Parent { get; set; }
    public int Status { get; set; }
    public DateTime CreatedDate { get; set; }

    [Ignore]
    public List<int> Ids { get; set; } = new();

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
