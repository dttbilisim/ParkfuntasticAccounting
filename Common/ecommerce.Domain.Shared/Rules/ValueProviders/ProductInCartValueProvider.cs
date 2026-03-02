using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Identity;
using ecommerce.Core.Rules.Fields;
using ecommerce.Core.Utils;
using ecommerce.EFCore.Context;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Domain.Shared.Rules.ValueProviders;

public class ProductInCartValueProvider : FieldDefinitionValueProvider
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly CurrentUser _currentUser;

    public ProductInCartValueProvider(IUnitOfWork<ApplicationDbContext> context, CurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public override async Task<object?> GetAsync(FieldDefinition fieldDefinition)
    {
        var productIds = await _context.GetRepository<CartItem>()
            .GetAll(true)
            .Where(x => x.UserId == _currentUser.UserId && x.Status == (int) EntityStatus.Active)
            .Select(x => x.ProductId)
            .ToListAsync();

        return productIds;
    }
}