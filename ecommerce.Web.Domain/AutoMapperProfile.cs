using AutoMapper;
using ecommerce.Core.Entities;
using ecommerce.Web.Domain.Dtos.Cart;

namespace ecommerce.Web.Domain;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<CartResult, CartDto>();
        CreateMap<CartCargoPropertyResult, CartCargoPropertyDto>();
        CreateMap<CartCargoResult, CartCargoDto>()
            .ForMember(d => d.CargoType, o => o.MapFrom(s => (int)s.CargoType));
        CreateMap<CartSellerResult, CartSellerDto>();
        CreateMap<CartItem, CartItemDto>();
        CreateMap<Discount, CartAppliedDiscountDto>();
        CreateMap<DiscountSummary, CartDiscountSummaryDto>();
    }
}


