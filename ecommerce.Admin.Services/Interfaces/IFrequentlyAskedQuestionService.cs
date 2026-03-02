using ecommerce.Admin.Domain.Dtos.FrequentlyAskedQuestionsDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Interfaces
{
    public interface IFrequentlyAskedQuestionService
    {
        public Task<IActionResult<Paging<IQueryable<FrequentlyAskedQuestionListDto>>>> GetFrequentlyAskedQuestions(PageSetting pager);
        public Task<IActionResult<List<FrequentlyAskedQuestionListDto>>> GetFrequentlyAskedQuestions();
        Task<IActionResult<Empty>> UpsertFrequentlyAskedQuestion(AuditWrapDto<FrequentlyAskedQuestionUpsertDto> model);
        Task<IActionResult<Empty>> DeleteFrequentlyAskedQuestion(AuditWrapDto<FrequentlyAskedQuestionDeleteDto> model);
        Task<IActionResult<FrequentlyAskedQuestionUpsertDto>> GetFrequentlyAskedQuestionById(int Id);
    }
}
