using ecommerce.Admin.EFCore;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Rules.Fields;
using ecommerce.EFCore.Context;
namespace ecommerce.Domain.Shared.Rules.OptionProviders;
public class SellerOptionProvider : FieldDefinitionValueOptionProvider
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;

    public SellerOptionProvider(IUnitOfWork<ApplicationDbContext> context)
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
        var query = _context.GetRepository<Company>().GetAll(true);

        if (selected is { Length: > 0 })
        {
            skip = 0;
            take = selected.Length;
            query = query.Where(c => selected.Contains(c.Id.ToString()));
        }
        else if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(
                c => (c.AccountName != null && c.AccountName.ToLower().Contains(search.ToLower()))
                     || (c.FirstName != null && c.FirstName.ToLower().Contains(search.ToLower()))
                     || (c.LastName != null && c.LastName.ToLower().Contains(search.ToLower()))
            );
        }

        var companies = await query.ToPagedListAsync(skip > 0 ? skip / take : 0, take);

        return new FieldDefinitionValueSelectPagedList
        {
            Data = companies.Items.Select(c => new FieldDefinitionValueSelectListOption(c.AccountName ?? $"{c.FirstName} {c.LastName}", c.Id.ToString())).ToList(),
            Count = companies.TotalCount
        };
    }
}