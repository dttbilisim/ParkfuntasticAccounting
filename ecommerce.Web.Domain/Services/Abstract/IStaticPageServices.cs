using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Web.Domain.Services.Abstract;

public interface IStaticPageServices
{
    Task<IActionResult<StaticPage>> GetStaticPageAsync(StaticPageType type);

}