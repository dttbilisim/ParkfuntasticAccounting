using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
namespace ecommerce.Admin.Domain.Dtos.ProductActiveArcticleDto
{
    [AutoMap(typeof(ProductActiveArticleItem), ReverseMap = true)]
    public class ProductActiveArticleUpsertDto
    {
        public int? Id { get; set; }
        public int? ProductId { get; set; }
        public int? ActiveArticleId { get; set; }
        public int? ScaleUnitId { get; set; }
        public decimal? Amount { get; set; }

        /// <summary>
        /// Ölçek Sayusu
        /// </summary>
        public int? ScaleCount { get; set; }

        /// <summary>
        /// Ölçek Tipi
        /// </summary>
        public ScaleType? ScaleType { get; set; }




        [Ignore]
        public bool StatusBool { get; set; }
    }
}
