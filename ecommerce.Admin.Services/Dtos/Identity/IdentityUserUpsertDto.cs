using ecommerce.Admin.Domain.Dtos.HierarchicalDto;

namespace ecommerce.Admin.Domain.Dtos.Identity;

public class IdentityUserUpsertDto
{
    public int? Id { get; set; }

    public string UserName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public List<int> Roles { get; set; } = new();

    public List<int> MenuIds { get; set; } = new();

    public string? Password { get; set; }

    public string? PasswordConfirm { get; set; }

    public int? SalesPersonId { get; set; }

    public List<UserBranchUpsertDto> Branches { get; set; } = new();
}