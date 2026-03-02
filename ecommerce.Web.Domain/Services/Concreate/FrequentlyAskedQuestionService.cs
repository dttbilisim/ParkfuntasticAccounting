using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.Web.Domain.Services.Abstract;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ecommerce.Web.Domain.Services.Concreate;

public class FrequentlyAskedQuestionService : IFrequentlyAskedQuestionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<StaticPageServices> _logger;
    public FrequentlyAskedQuestionService(ApplicationDbContext dbContext, ILogger<StaticPageServices> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    public async Task<IActionResult<List<FrequentlyAskedQuestion>>> GetAllAsync(SSSAndBlogGroup group)
    {
        var rs = OperationResult.CreateResult<List<FrequentlyAskedQuestion>>();

        try
        {
            var list = await _dbContext.FrequentlyAskedQuestions
                .AsNoTracking()
                .Where(x => x.Status == 1 && x.Group == group)
                .OrderBy(x => x.Order)
                .ThenBy(x => x.Id)
                .ToListAsync();

            rs.Result = list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FrequentlyAskedQuestionService.GetAllAsync exception");
            rs.AddSystemError(ex.ToString());
        }

        return rs;
    }
}