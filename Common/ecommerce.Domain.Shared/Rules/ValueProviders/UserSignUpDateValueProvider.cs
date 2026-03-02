using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Identity;
using ecommerce.Core.Rules.Fields;
using ecommerce.EFCore.Context;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Domain.Shared.Rules.ValueProviders;

public class UserSignUpDateValueProvider : FieldDefinitionValueProvider
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly CurrentUser _currentUser;

    public UserSignUpDateValueProvider(IUnitOfWork<ApplicationDbContext> context, CurrentUser currentUser)
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

        var createdDate = await _context.GetRepository<ApplicationUser>()
            .GetAll(true)
            .Where(x => x.Id == _currentUser.GetId())
            .Select(x => x.RegisterDate)
            .FirstOrDefaultAsync();

        return createdDate;
    }
}