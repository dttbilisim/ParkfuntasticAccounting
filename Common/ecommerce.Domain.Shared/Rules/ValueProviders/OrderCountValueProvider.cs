using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Identity;
using ecommerce.Core.Rules.Fields;
using ecommerce.Core.Utils;
using ecommerce.EFCore.Context;

namespace ecommerce.Domain.Shared.Rules.ValueProviders;

public class OrderCountValueProvider : FieldDefinitionValueProvider
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly CurrentUser _currentUser;

    public OrderCountValueProvider(IUnitOfWork<ApplicationDbContext> context, CurrentUser currentUser)
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

        var orderCount = await _context.GetRepository<Orders>()
            .CountAsync(
                o => o.CompanyId == _currentUser.GetUserId()
                     && o.OrderStatusType == OrderStatusType.OrderSuccess
            );

        return orderCount;
    }
}