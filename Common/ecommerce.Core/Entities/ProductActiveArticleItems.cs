using ecommerce.Core.Entities.Base;
using ecommerce.Core.Utils;
using System.ComponentModel.DataAnnotations.Schema;
namespace ecommerce.Core.Entities
{
    public class ProductActiveArticleItem : AuditableEntity<int>
    {
        public int ProductId { get; set; }
        public int ActiveArticleId { get; set; }
        public int ScaleUnitId { get; set; }
        public decimal Amount { get; set; }

        /// <summary>
        /// Ölçek Sayusu
        /// </summary>
        public int ScaleCount { get; set; }

        /// <summary>
        /// Ölçek Tipi
        /// </summary>
        public ScaleType ScaleType { get; set; }

        #region Navigations
        [ForeignKey("ProductId")]
        public Product Product { get; set; }

        [ForeignKey("ActiveArticleId")]
        public ActiveArticle ActiveArticle { get; set; }
        
        [ForeignKey("ScaleUnitId")]
        public ScaleUnit ScaleUnit { get; set; }
        #endregion
    }
}
