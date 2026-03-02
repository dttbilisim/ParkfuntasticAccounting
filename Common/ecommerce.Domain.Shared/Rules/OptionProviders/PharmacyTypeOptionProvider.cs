using ecommerce.Admin.EFCore;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Rules.Fields;
using ecommerce.EFCore.Context;

namespace ecommerce.Domain.Shared.Rules.OptionProviders;

public class PharmacyTypeOptionProvider : FieldDefinitionValueOptionProvider
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;

    public PharmacyTypeOptionProvider(IUnitOfWork<ApplicationDbContext> context)
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
        var query = _context.GetRepository<PharmacyType>().GetAll(true);

        if (selected is { Length: > 0 })
        {
            skip = 0;
            take = selected.Length;
            query = query.Where(c => selected.Contains(c.Id.ToString()));
        }
        else if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(c => c.Name.ToLower().Contains(search.ToLower()));
        }

        var products = await query.ToPagedListAsync(skip > 0 ? skip / take : 0, take);

        return new FieldDefinitionValueSelectPagedList
        {
            Data = products.Items.Select(c => new FieldDefinitionValueSelectListOption(c.Name, c.Id.ToString())).ToList(),
            Count = products.TotalCount
        };
    }
}