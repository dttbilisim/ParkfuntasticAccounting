using System.ComponentModel.DataAnnotations;

namespace ecommerce.Admin.Domain.Dtos.Role;

public class RoleUpsertDto
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Rol adı zorunludur.")]
    public string Name { get; set; } = null!;
}
