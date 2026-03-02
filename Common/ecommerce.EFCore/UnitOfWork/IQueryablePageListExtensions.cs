using ecommerce.Core.Utils.PagedList;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Admin.EFCore;

public static class IQueryablePageListExtensions
{
    public static async Task<IPagedList<T>> ToPagedListAsync<T>(
        this IQueryable<T> source,
        int pageIndex,
        int pageSize,
        int indexFrom = 0,
        CancellationToken cancellationToken = default)
    {
        if (indexFrom > pageIndex)
        {
            throw new ArgumentException(
                $"indexFrom: {indexFrom} > pageIndex: {pageIndex}, must indexFrom <= pageIndex"
            );
        }

        var count = await source.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await source.Skip((pageIndex - indexFrom) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var pagedList = new PagedList<T>(items, pageIndex, pageSize, indexFrom, count);

        return pagedList;
    }
}