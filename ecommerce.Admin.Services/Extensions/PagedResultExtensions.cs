using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using ecommerce.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Admin.Domain.Extensions
{
    public static class PagedResultExtensions
    {
        public static int DefaultMaxResultCount { get; set; } = 10;

        public static int MaxResultCount { get; set; } = 1000;

        public static int ExportMaxResultCount { get; set; } = 100000;

        public static Task<Paging<List<TResult>>> ToPagedResultAsync<TResult>(
            this IQueryable<TResult> queryable,
            PageSetting request)
        {
            return queryable.ToPagedResultAsync<TResult>(request, query: null);
        }

        public static Task<Paging<List<TResult>>> ToPagedResultAsync<TResult>(
            this IQueryable queryable,
            PageSetting request,
            IMapper objectMapper,
            Func<IQueryable, IQueryable<TResult>>? resultQuery = null)
        {
            return queryable.ToPagedResultAsync<TResult>(
                request,
                q =>
                {
                    var projection = q.ProjectTo<TResult>(objectMapper.ConfigurationProvider);

                    return resultQuery != null ? resultQuery(projection) : projection;
                }
            );
        }

        public static Task<Paging<List<TResult>>> ToPagedResultAsync<TSource, TResult>(
            this IQueryable<TSource> queryable,
            PageSetting request,
            Expression<Func<TSource, TResult>> selector,
            Func<IQueryable, IQueryable<TResult>>? resultQuery = null)
        {
            return queryable.ToPagedResultAsync<TResult>(
                request,
                q =>
                {
                    var query = q.Cast<TSource>().Select(selector);

                    return resultQuery != null ? resultQuery(query) : query;
                }
            );
        }

        public static async Task<Paging<List<TResult>>> ToPagedResultAsync<TResult>(
            this IQueryable queryable,
            PageSetting request,
            Func<IQueryable, IQueryable<TResult>>? query)
        {
            var skip = Math.Max(request.Skip ?? 0, 0);
            var take = request.Take >= DefaultMaxResultCount && request.Take <= MaxResultCount
                ? request.Take.Value
                : DefaultMaxResultCount;

            if (request.Export)
            {
                skip = 0;
                take = ExportMaxResultCount;
            }

            if (string.IsNullOrEmpty(request.OrderBy))
            {
                var idProperty = queryable.ElementType.GetProperty("Id");
                if (idProperty != null)
                {
                    queryable = queryable.OrderBy("Id");
                }
            }

            var resultQueryable = query != null ? query(queryable) : queryable.Cast<TResult>();

            if (!string.IsNullOrEmpty(request.Filter))
            {
                request.Filter = Regex.Replace(request.Filter, @"np\((?<PropertyName>[^\)]+)\)", "${PropertyName}");
                resultQueryable = resultQueryable.Where(request.Filter);
            }

            if (!string.IsNullOrEmpty(request.OrderBy))
            {
                request.OrderBy = Regex.Replace(request.OrderBy, @"np\((?<PropertyName>[^\)]+)\)", "${PropertyName}");
                resultQueryable = resultQueryable.OrderBy(request.OrderBy);
            }

            var count = await resultQueryable.CountAsync();

            // serialize enumeration after count to avoid overlapping commands on same DbContext
            var paged = resultQueryable.Skip(skip).Take(take);

            var result = await paged.ToListAsync();

            return new Paging<List<TResult>>
            {
                Data = result,
                DataCount = count,
            };
        }
    }
}