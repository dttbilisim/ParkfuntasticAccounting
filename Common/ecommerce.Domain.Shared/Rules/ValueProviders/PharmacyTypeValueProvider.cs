using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Identity;
using ecommerce.Core.Rules.Fields;
using ecommerce.EFCore.Context;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Domain.Shared.Rules.ValueProviders;

public class PharmacyTypeValueProvider : FieldDefinitionValueProvider
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly CurrentUser _currentUser;

    public PharmacyTypeValueProvider(IUnitOfWork<ApplicationDbContext> context, CurrentUser currentUser)
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

        var pharmacyType = await _context.GetRepository<Company>()
            .GetAll(true)
            .Where(x => x.Id == _currentUser.GetUserId())
            .Select(x => x.PharmacyTypeId)
            .FirstOrDefaultAsync();

        return pharmacyType;
    }
}