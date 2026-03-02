using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Identity;
using ecommerce.Core.Rules.Fields;
using ecommerce.Core.Utils;
using ecommerce.EFCore.Context;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Domain.Shared.Rules.ValueProviders;

public class ProductFromCategoryInCartValueProvider : FieldDefinitionValueProvider
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly CurrentUser _currentUser;

    public ProductFromCategoryInCartValueProvider(IUnitOfWork<ApplicationDbContext> context, CurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public override async Task<object?> GetAsync(FieldDefinition fieldDefinition)
    {
        var categoryIds = await _context.GetRepository<CartItem>()
            .GetAll(true)
            .Include(c => c.Product.Categories)
            .Where(x => x.UserId == _currentUser.GetUserId() && x.Status == (int) EntityStatus.Active)
            .SelectMany(x => x.Product.Categories)
            .Select(x => x.CategoryId)
            .Distinct()
            .ToListAsync();

        return categoryIds;
    }
}