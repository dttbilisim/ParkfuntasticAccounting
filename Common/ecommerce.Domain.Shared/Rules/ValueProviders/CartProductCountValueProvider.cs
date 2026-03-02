using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Identity;
using ecommerce.Core.Rules.Fields;
using ecommerce.Core.Utils;
using ecommerce.EFCore.Context;

namespace ecommerce.Domain.Shared.Rules.ValueProviders;

public class CartProductCountValueProvider : FieldDefinitionValueProvider
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly CurrentUser _currentUser;

    public CartProductCountValueProvider(IUnitOfWork<ApplicationDbContext> context, CurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public override async Task<object?> GetAsync(FieldDefinition fieldDefinition)
    {
        var cartTotalItems =await _context.GetRepository<CartItem>()
            .CountAsync(c => c.UserId == _currentUser.GetUserId() && c.Status == (int) EntityStatus.Active);

        return cartTotalItems;
    }
}