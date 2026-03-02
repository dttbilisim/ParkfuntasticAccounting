using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.Web.Domain.Dtos;
using ecommerce.Web.Domain.Services.Abstract;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Web.Domain.Services.Concreate;

public class DiscountService : IDiscountService
{
    private readonly ApplicationDbContext _context;

    public DiscountService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult<List<DiscountDto>>> GetActiveDiscountsAsync()
    {
        var rs = OperationResult.CreateResult<List<DiscountDto>>();
        try
        {
            var now = DateTime.UtcNow;
            
            var discounts = await _context.Discounts
                .Where(d => d.Status == 1 &&
                           (!d.StartDate.HasValue || d.StartDate.Value <= now) &&
                           (!d.EndDate.HasValue || d.EndDate.Value >= now))
                .OrderByDescending(d => d.CreatedDate)
                .Select(d => new DiscountDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Description = d.Description,
                    CouponDescription = d.CouponDescription,
                    ImagePath = d.ImagePath,
                    CampaignLink = d.CampaignLink,
                    StartDate = d.StartDate,
                    EndDate = d.EndDate,
                    RequiresCouponCode = d.RequiresCouponCode,
                    CouponCode = d.CouponCode,
                    UsePercentage = d.UsePercentage,
                    DiscountPercentage = d.DiscountPercentage,
                    DiscountAmount = d.DiscountAmount,
                    MaximumDiscountAmount = d.MaximumDiscountAmount,
                    IsActive = true
                })
                .ToListAsync();

            rs.Result = discounts;
        }
        catch (Exception ex)
        {
            rs.AddSystemError($"İndirimler yüklenirken hata oluştu: {ex.Message}");
        }

        return rs;
    }

    public async Task<IActionResult<DiscountDto>> GetDiscountByIdAsync(int id)
    {
        var rs = OperationResult.CreateResult<DiscountDto>();
        try
        {
            var discount = await _context.Discounts
                .Where(d => d.Id == id)
                .Select(d => new DiscountDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Description = d.Description,
                    CouponDescription = d.CouponDescription,
                    ImagePath = d.ImagePath,
                    StartDate = d.StartDate,
                    EndDate = d.EndDate,
                    RequiresCouponCode = d.RequiresCouponCode,
                    CouponCode = d.CouponCode,
                    UsePercentage = d.UsePercentage,
                    DiscountPercentage = d.DiscountPercentage,
                    DiscountAmount = d.DiscountAmount,
                    MaximumDiscountAmount = d.MaximumDiscountAmount,
                    IsActive = d.Status == 1
                })
                .FirstOrDefaultAsync();

            if (discount == null)
            {
                rs.AddSystemError("İndirim bulunamadı");
            }
            else
            {
                rs.Result = discount;
            }
        }
        catch (Exception ex)
        {
            rs.AddSystemError($"İndirim yüklenirken hata oluştu: {ex.Message}");
        }

        return rs;
    }
}
