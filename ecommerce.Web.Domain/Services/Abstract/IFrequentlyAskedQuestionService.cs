using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Web.Domain.Services.Abstract;

public interface IFrequentlyAskedQuestionService
{
        Task<IActionResult<List<FrequentlyAskedQuestion>>> GetAllAsync(SSSAndBlogGroup group);


}