using ecommerce.Core.Models;
namespace ecommerce.Admin.Domain.Interfaces
{
    public interface IRadzenPagerService<T> where T : class
    {
        Paging<IQueryable<T>> MakeDataQueryable(IQueryable<T> query, PageSetting pager);
        Paging<IQueryable<T>> MakeDataODataQuery(IQueryable<T> query, PageSetting pager);
    }
}
