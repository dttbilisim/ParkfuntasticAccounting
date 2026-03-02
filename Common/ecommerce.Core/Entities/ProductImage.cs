using ecommerce.Core.Entities.Base;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace ecommerce.Core.Entities
{
    public class ProductImage : AuditableEntity<int>
    {
        public int ProductId { get; set; }
        public int Order { get; set; }
        public string FileGuid { get; set; }
        public string FileName { get; set; }
        public string Root { get; set; }
       

        #region Navigations

        [ForeignKey("ProductId")]
        [JsonIgnore]
        public Product Product { get; set; }

        #endregion

   
    }
}
