using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
namespace ecommerce.Admin.Domain.Dtos.ProductActiveArcticleDto
{
    [AutoMap(typeof(ProductActiveArticleItem))]
    public class ProductActiveArticleListDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int ActiveArticleId { get; set; }
        public int ActiveArticleUnitFormId { get; set; }
        public int ScaleUnitId { get; set; }
        public decimal Amount { get; set; }

        /// <summary>
        /// Ölçek Sayusu
        /// </summary>
        public int ScaleCount { get; set; }

        /// <summary>
        /// Ölçek Tipi
        /// </summary>
        [Ignore]
        public string ScaleTypeStr
        {
            get
            {
                var returnValue = "";
                if (ScaleType == ScaleType.Scale)
                    returnValue = "Ölçek";
                else if (ScaleType == ScaleType.Tablet)
                    returnValue = "Tablet";
                return returnValue;
            }
        }

        public EntityStatus Status { get; set; }

        public ScaleType ScaleType { get; set; }

        public ActiveArticle ActiveArticle { get; set; }
        public ScaleUnit ScaleUnit { get; set; }

    }
}
