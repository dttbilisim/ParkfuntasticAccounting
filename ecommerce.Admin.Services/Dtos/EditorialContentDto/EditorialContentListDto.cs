using AutoMapper;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.EditorialContentDto
{
    [AutoMap(typeof(EditorialContent))]
    public class EditorialContentListDto
    {
        public int Id { get; set; }

        public EditorialContentType Type { get; set; }

        public string Slug { get; set; } = null!;

        public string Title { get; set; } = null!;

        public string Thumbnail { get; set; } = null!;

        public int Order { get; set; }
        public bool IsDefault{get;set;}

        public int Status { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? ModifiedDate { get; set; }
        
        public DateTime PublishDate { get; set; }
        public DateTime? EndDate{get;set;}
    }
}