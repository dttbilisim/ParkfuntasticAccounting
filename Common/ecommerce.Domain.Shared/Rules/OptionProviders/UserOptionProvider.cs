using ecommerce.Admin.EFCore;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Rules.Fields;
using ecommerce.EFCore.Context;

namespace ecommerce.Domain.Shared.Rules.OptionProviders;

public class UserOptionProvider : FieldDefinitionValueOptionProvider
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;

    public UserOptionProvider(IUnitOfWork<ApplicationDbContext> context)
    {
        _context = context;
    }

    public override async Task<FieldDefinitionValueSelectPagedList> GetAsync(
        FieldDefinition fieldDefinition,
        int skip = 0,
        int take = 10,
        string? search = null,
        string[]? selected = null)
    {
        var query = _context.GetRepository<ApplicationUser>().GetAll(true);

        if (selected is { Length: > 0 })
        {
            skip = 0;
            take = selected.Length;
            query = query.Where(c => selected.Contains(c.Id.ToString()));
        }
        else if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(
                c => (c.Email != null && c.Email.ToLower().Contains(search.ToLower()))
                     || c.FirstName.ToLower().Contains(search.ToLower())
                     || c.LastName.ToLower().Contains(search.ToLower())
            );
        }

        var users = await query.ToPagedListAsync(skip > 0 ? skip / take : 0, take);

        return new FieldDefinitionValueSelectPagedList
        {
            Data = users.Items.Select(c => new FieldDefinitionValueSelectListOption(c.Email ?? $"{c.FirstName} {c.LastName}", c.Id.ToString())).ToList(),
            Count = users.TotalCount
        };
    }
}