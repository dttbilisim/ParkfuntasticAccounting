using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Core.Entities;
namespace ecommerce.Admin.Domain.Concreate;
public class OtoIsmailProductMapper : IProductMapper<ProductOtoIsmail>
{
    public Product Map(ProductOtoIsmail item)
    {
        return new Product
        {
            Name = item.Ad,
            Description = item.Ad,
            Barcode = string.Join(",", new[] { item.Barkod1, item.Barkod2, item.Barkod3 }
                                  .Where(x => !string.IsNullOrWhiteSpace(x))),
            WebKeyword = string.Join(",", new[] { item.Ad, item.Kod, item.OrjinalKod, item.Marka, item.GrupKodu, item.Oem }
                                  .Where(x => !string.IsNullOrWhiteSpace(x))),
            Price = (decimal)item.Fiyat1,
            // Removed: AvgPrice (calculated from SellerItems), Seller Id (moved to SellerItems), AdvertCount
            Status = item.Status,
            CreatedId = item.CreatedId,
            ModifiedId = item.ModifiedId,
            DeletedId = item.DeletedId,
            CreatedDate = item.CreatedDate,
            ModifiedDate = item.ModifiedDate,
            DeletedDate = item.DeletedDate,
            CargoDesi = 0,
            Height = 0,
            Length = 0,
            Width = 0
        };
    }
}
