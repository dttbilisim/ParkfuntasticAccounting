using AutoMapper;
using ecommerce.Core.Entities.Authentication;

namespace ecommerce.Admin.Domain.Dtos.Identity;

[AutoMap(typeof(ApplicationRole))]
public class IdentityRoleListDto
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;
}