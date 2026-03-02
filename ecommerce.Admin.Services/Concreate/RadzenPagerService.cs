using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Core.Models;
using Microsoft.EntityFrameworkCore;
using ODataQuery;
using System.Linq.Dynamic.Core;
namespace ecommerce.Admin.Domain.Concreate
{
    public class RadzenPagerService<T> : IRadzenPagerService<T> where T : class
    {


        public Paging<IQueryable<T>> MakeDataODataQuery(IQueryable<T> query, PageSetting pager)
        {
            Paging<IQueryable<T>> paging = new();
            paging.Data = query;


            if (!string.IsNullOrEmpty(pager.Filter))
                paging.Data = paging.Data.ODataFilter(pager.Filter);


            if (!string.IsNullOrEmpty(pager.OrderBy))
                paging.Data = paging.Data.ODataOrderBy(pager.OrderBy);

            if (paging.Data != null)
                paging.Data = paging.Data.Skip(pager.Skip.Value).Take(pager.Take.Value).AsQueryable();


            paging.Data = (IQueryable<T>)paging.Data;
            paging.DataCount = query == null ? 0 : query.Count();
            return paging;
        }

        public Paging<IQueryable<T>> MakeDataQueryable(IQueryable<T> query, PageSetting pager)
        {
            Paging<IQueryable<T>> paging = new();
            var data = query ?? Enumerable.Empty<T>().AsQueryable();

            if (!string.IsNullOrEmpty(pager.Filter))
                data = data.Where(pager.Filter);

            if (!string.IsNullOrEmpty(pager.OrderBy))
                data = data.ODataOrderBy(pager.OrderBy);

            var count = data.Count();

            data = data.Skip(pager.Skip.Value).Take(pager.Take.Value).AsQueryable();

            paging.Data = data;
            paging.DataCount = query == null ? 0 : count;
            return paging;
        }


    }
}
