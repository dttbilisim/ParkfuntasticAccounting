using AutoMapper;
using ecommerce.Admin.Domain.Dtos.CompanyDto;

namespace ecommerce.Admin.Domain.Dtos.DiscountDto;

[AutoMap(typeof(DiscountCompanyCouponUpsertDto))]
public class DiscountCompanyCouponUpsertDto
{
    public int? Id { get; set; }

    public int CompanyId { get; set; }

    public CompanyListDto Company { get; set; } = null!;

    public string CouponCode { get; set; } = null!;
}