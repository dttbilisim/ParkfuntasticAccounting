using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;

namespace ecommerce.Admin.Domain.Dtos.CategoryDto
{
    [AutoMap(typeof(Category), ReverseMap = true)]
    public class CategoryUpsertDto
    {
        public int? Id { get; set; }
        public string Name { get; set; } = null!;
        public int? ParentId { get; set; }
        public DateTime CreatedDate { get; set; }
        public int Status { get; set; }
        public bool IsMainPage { get; set; }
        public bool IsMainSlider{get;set;}
        public int ? SubCategoryCount{get;set;} = 0;
        public int ? Height{get;set;} = 0;
        public int Order { get; set; }
        public string ? ImageUrl{get;set;}

        [Ignore]
        public bool StatusBool { get; set; }
    }
}
