using AutoMapper;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
namespace ecommerce.Admin.Domain.Dtos.FrequentlyAskedQuestionsDto
{
    [AutoMap(typeof(FrequentlyAskedQuestion),ReverseMap =true)]
    public class FrequentlyAskedQuestionUpsertDto
    {
        public int? Id { get; set; }
        public SSSAndBlogGroup Group { get; set; }
        public int? ParentId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int? Order{get;set;}
    }
}
