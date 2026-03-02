using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;

namespace ecommerce.Admin.Domain.Dtos.CategoryDto
{
    [AutoMap(typeof(Category))]
    public class CategoryListDto
    {
        public int Id { get; set; }
        public int Order { get; set; }
        public string IdStr
        {
            get
            {
                return Id.ToString();
            }
        }
        public string Name { get; set; } = null!;
        public int? ParentId { get; set; }
        public string? ParentName { get { return Parent?.Name; } }
        public CategoryListDto? Parent { get; set; }
        public int Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public int ? SubCategoryCount{get;set;} = 0;
        public int ? Height{get;set;} = 0;
        public string ? ImageUrl{get;set;}

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
}
