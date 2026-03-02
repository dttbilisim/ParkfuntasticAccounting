using ecommerce.Core.Identity;
using ecommerce.Core.Rules.Fields;

namespace ecommerce.Domain.Shared.Rules.ValueProviders;

public class UserValueProvider : FieldDefinitionValueProvider
{
    private readonly CurrentUser _currentUser;

    public UserValueProvider(CurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public override Task<object?> GetAsync(FieldDefinition fieldDefinition)
    {
        return Task.FromResult<object?>(_currentUser.Id);
    }
}