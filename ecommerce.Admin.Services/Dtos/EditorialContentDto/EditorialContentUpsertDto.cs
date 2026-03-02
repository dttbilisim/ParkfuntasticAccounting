using AutoMapper;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.EditorialContentDto
{
    [AutoMap(typeof(EditorialContent), ReverseMap = true)]
    public class EditorialContentUpsertDto
    {
        public int? Id { get; set; }

        public EditorialContentType? Type { get; set; }

        public string? Slug { get; set; }

        public string Title { get; set; } = null!;

        public string? Content { get; set; }

        public string? Category { get; set; }

        public string? Thumbnail { get; set; }

        public string? Video { get; set; }
        public bool IsDefault{get;set;}

        public DateTime? PublishDate { get; set; }
        public DateTime? EndDate{get;set;}

        public int Order { get; set; }

        public int Status { get; set; }
    }
}