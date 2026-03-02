using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Identity;
using ecommerce.Core.Rules.Fields;
using ecommerce.Core.Utils;
using ecommerce.EFCore.Context;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Domain.Shared.Rules.ValueProviders;

public class OrderSalesAmountValueProvider : FieldDefinitionValueProvider
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly CurrentUser _currentUser;

    public OrderSalesAmountValueProvider(IUnitOfWork<ApplicationDbContext> context, CurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public override async Task<object?> GetAsync(FieldDefinition fieldDefinition)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return 0;
        }

        var totalSpentAmount = await _context.GetRepository<Orders>()
            .GetAll(true)
            .Where(
                o => o.SellerId == _currentUser.GetUserId()
                     && o.OrderStatusType == OrderStatusType.OrderSuccess
            )
            .SumAsync(o => o.OrderTotal);

        return totalSpentAmount;
    }
}