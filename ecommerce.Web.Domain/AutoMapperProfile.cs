using AutoMapper;
using ecommerce.Core.Entities;
using ecommerce.Web.Domain.Dtos.Cart;

namespace ecommerce.Web.Domain;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<CartResult, CartDto>()
            .AfterMap((src, dest) => dest.Currency = src.Sellers?.FirstOrDefault()?.Items?.FirstOrDefault(i => i.ProductSellerItem != null)?.ProductSellerItem?.Currency);
        CreateMap<CartCargoPropertyResult, CartCargoPropertyDto>();
        CreateMap<CartCargoResult, CartCargoDto>()
            .ForMember(d => d.CargoType, o => o.MapFrom(s => (int)s.CargoType));
        CreateMap<CartSellerResult, CartSellerDto>()
            .AfterMap((src, dest) => dest.Currency = src.Items?.FirstOrDefault(i => i.ProductSellerItem != null)?.ProductSellerItem?.Currency);
        CreateMap<CartItem, CartItemDto>()
            .ForMember(d => d.IsPackageProduct, o => o.MapFrom(s => s.Product != null && s.Product.IsPackageProduct))
            .ForMember(d => d.VisitDate, o => o.MapFrom(s => s.VisitDate))
            .ForMember(d => d.Currency, o => o.MapFrom(s => s.ProductSellerItem != null ? s.ProductSellerItem.Currency : null))
            .ForMember(d => d.PackageProductItems, o => o.MapFrom(s =>
                s.Product != null && s.Product.IsPackageProduct && s.Product.ProductSaleItemsAsRef != null && s.Product.ProductSaleItemsAsRef.Any()
                    ? s.Product.ProductSaleItemsAsRef.Select(ps => new CartPackageItemDto
                    {
                        ProductId = ps.ProductId,
                        ProductName = ps.Product != null ? ps.Product.Name : "",
                        Price = ps.Price,
                        Quantity = s.PackageItemQuantities != null && s.PackageItemQuantities.ContainsKey(ps.ProductId) ? s.PackageItemQuantities[ps.ProductId] : 1
                    }).ToList()
                    : new List<CartPackageItemDto>()));
        CreateMap<Discount, CartAppliedDiscountDto>();
        CreateMap<DiscountSummary, CartDiscountSummaryDto>();
    }
}


