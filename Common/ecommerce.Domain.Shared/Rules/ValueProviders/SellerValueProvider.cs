using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Identity;
using ecommerce.Core.Rules.Fields;
using ecommerce.EFCore.Context;
using Microsoft.EntityFrameworkCore;
namespace ecommerce.Domain.Shared.Rules.ValueProviders;
public class SellerValueProvider:FieldDefinitionValueProvider{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly CurrentUser _currentUser;

    public SellerValueProvider(IUnitOfWork<ApplicationDbContext> context, CurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public override async Task<object?> GetAsync(FieldDefinition fieldDefinition)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return null;
        }

        var seller = await _context.GetRepository<CartItem>()
            .GetAll(true).Include(x=>x.ProductSellerItem)
            .Where(x=>x.UserId==_currentUser.GetUserId() && x.Status==1 && x.ProductSellerItem.Status==1)
            .Select(x => x.ProductSellerItem.SellerId)
            .Distinct()
            .ToListAsync();
           

        return seller;
    }
}
