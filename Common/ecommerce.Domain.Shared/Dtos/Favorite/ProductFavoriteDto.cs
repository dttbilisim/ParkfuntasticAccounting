using ecommerce.Core.Entities;
namespace ecommerce.Domain.Shared.Dtos.Favorite;
public class ProductFavoriteDto{
    public int Id { get; set; }  // ProductId
    public int? SellerItemId { get; set; }  // İlk SellerItemId (en düşük fiyatlı)
    public string Name{get;set;}
    public string ? Description { get; set; }

    public string ? BrandName { get; set; }
    public string ? CategoryName { get; set; }

    public int? UserId { get; set; }
    public string ? FileName { get; set; }
    public int Status { get; set; }
    public string? DocumentUrl { get; set; }
    public string? SourceId { get; set; }
}
