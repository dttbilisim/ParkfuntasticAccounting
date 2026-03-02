using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.Web.Domain.Services.Abstract;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ecommerce.Web.Domain.Services.Concreate;

public class StaticPageServices: IStaticPageServices
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<StaticPageServices> _logger;

    public StaticPageServices(ApplicationDbContext dbContext, ILogger<StaticPageServices> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IActionResult<StaticPage>> GetStaticPageAsync(StaticPageType type)
    {
        var rs = OperationResult.CreateResult<StaticPage>();

        try
        {
            var data = await _dbContext.AboutUs.FirstOrDefaultAsync(x => x.Status == 1 && x.StaticPageType == type);
            if (data == null) return rs;

            rs.Result = data;
        }
        catch (Exception ex)
        {
            _logger.LogError("StaticPageServices.GetStaticPageAsync Exception: {Exception}", ex);
            rs.AddSystemError(ex.ToString());
        }

        return rs;
    }
}