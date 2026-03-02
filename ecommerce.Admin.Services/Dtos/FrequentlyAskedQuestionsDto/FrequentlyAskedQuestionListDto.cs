using AutoMapper;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
namespace ecommerce.Admin.Domain.Dtos.FrequentlyAskedQuestionsDto
{
    [AutoMap(typeof(FrequentlyAskedQuestion))]
    public class FrequentlyAskedQuestionListDto
    {
        public int Id { get; set; }
        public int? ParentId { get; set; }
        public int? Order{get;set;}

        public string Name { get; set; }
        public string ParentName { get; set; }
        public SSSAndBlogGroup Group { get; set; }

    }
}
