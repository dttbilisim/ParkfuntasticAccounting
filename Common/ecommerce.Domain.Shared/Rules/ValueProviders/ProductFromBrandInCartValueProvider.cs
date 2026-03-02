using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Identity;
using ecommerce.Core.Rules.Fields;
using ecommerce.Core.Utils;
using ecommerce.EFCore.Context;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Domain.Shared.Rules.ValueProviders;

public class ProductFromBrandInCartValueProvider : FieldDefinitionValueProvider
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly CurrentUser _currentUser;

    public ProductFromBrandInCartValueProvider(IUnitOfWork<ApplicationDbContext> context, CurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public override async Task<object?> GetAsync(FieldDefinition fieldDefinition)
    {
        var brandIds = await _context.GetRepository<CartItem>()
            .GetAll(true)
            .Where(x => x.UserId == _currentUser.GetUserId() && x.Status == (int) EntityStatus.Active)
            .Include(c => c.Product)
            .Select(x => x.Product.BrandId)
            .Distinct()
            .ToListAsync();

        return brandIds;
    }
}