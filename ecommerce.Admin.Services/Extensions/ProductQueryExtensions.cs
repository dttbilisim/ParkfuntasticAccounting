using System;
using System.Linq;
using ecommerce.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Admin.Domain.Extensions
{
    public static class ProductQueryExtensions
    {
        public static IQueryable<Product> ApplySmartSearch(this IQueryable<Product> query, string search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return query;
            }

            var terms = search.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var term in terms)
            {
                var likeTerm = $"%{term}%";
                query = query.Where(x => EF.Functions.ILike(x.Name, likeTerm) || 
                                         (x.Barcode != null && EF.Functions.ILike(x.Barcode, likeTerm)) || 
                                         x.ProductUnits.Any(pu => pu.Barcode != null && EF.Functions.ILike(pu.Barcode, likeTerm)) ||
                                         x.ProductGroupCodes.Any(gc => gc.OemCode != null && EF.Functions.ILike(gc.OemCode, likeTerm)));
            }

            return query;
        }
    }
}
