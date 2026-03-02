using ecommerce.Admin.Domain.Dtos.SurveyDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Domain.Interfaces;

public interface ISurveyService
{
    Task<IActionResult<Paging<List<SurveyListDto>>>> GetSurveys(PageSetting pager);

    Task<IActionResult<List<SurveyListDto>>> GetSurveys();

    Task<IActionResult<Paging<List<SurveyAnswerListDto>>>> GetSurveyAnswers(int surveyId, PageSetting pager);

    Task<IActionResult<List<SurveyAnswerStatisticDto>>> GetSurveyAnswerStatistics(int surveyId);

    Task<IActionResult<SurveyUpsertDto>> GetSurveyById(int Id);

    Task<IActionResult<Empty>> UpsertSurvey(AuditWrapDto<SurveyUpsertDto> model);

    Task<IActionResult<Empty>> DeleteSurvey(AuditWrapDto<SurveyDeleteDto> model);
}