using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Identity;
using ecommerce.Core.Rules.Fields;
using ecommerce.Core.Utils;
using ecommerce.EFCore.Context;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Domain.Shared.Rules.ValueProviders;

public class OrderSoldProductsValueProvider : FieldDefinitionValueProvider
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly CurrentUser _currentUser;

    public OrderSoldProductsValueProvider(IUnitOfWork<ApplicationDbContext> context, CurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public override async Task<object?> GetAsync(FieldDefinition fieldDefinition)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return new List<int>();
        }

        var productIds = await _context.GetRepository<Orders>()
            .GetAll(true)
            .Include(o => o.OrderItems)
            .Where(
                o => o.SellerId == _currentUser.GetUserId()
                     && o.OrderStatusType == OrderStatusType.OrderSuccess
            )
            .SelectMany(o => o.OrderItems)
            .Select(oi => oi.ProductId)
            .Distinct()
            .ToListAsync();

        return productIds;
    }
}