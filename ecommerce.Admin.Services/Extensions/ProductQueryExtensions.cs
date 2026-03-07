using System;
using System.Linq;
using ecommerce.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Admin.Domain.Extensions
{
    public static class ProductQueryExtensions
    {
        /// <summary>
        /// Ürün adı ve barkod üzerinden arama yapar. Her kelime AND ile birleştirilir (tüm kelimeler eşleşmeli).
        /// </summary>
        public static IQueryable<Product> ApplySmartSearch(this IQueryable<Product> query, string search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return query;
            }

            var terms = search.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var term in terms)
            {
                var likeTerm = $"%{term}%";
                query = query.Where(x =>
                    (x.Name != null && EF.Functions.ILike(x.Name, likeTerm)) ||
                    (x.Barcode != null && EF.Functions.ILike(x.Barcode, likeTerm)));
            }

            return query;
        }
    }
}
