using AutoMapper;
using ecommerce.Core.Entities.Authentication;

namespace ecommerce.Admin.Domain.Dtos.Identity;

[AutoMap(typeof(ApplicationUser))]
public class IdentityUserListDto
{
    public int Id { get; set; }

    public string UserName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? PhoneNumber { get; set; }

    public List<IdentityRoleListDto> Roles { get; set; } = new();
    public string FullName => $"{FirstName} {LastName}".Trim();
    public bool IsPcPosUser { get; set; }
    public string? CompanyCode { get; set; }
    public string? CaseIds { get; set; }
    public bool IsEdit { get; set; }
    public int? UserType { get; set; }
}