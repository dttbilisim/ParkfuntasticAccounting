using AutoMapper;
using ecommerce.Core.Entities;

namespace ecommerce.Admin.Domain.Dtos.PopupDto;

[AutoMap(typeof(Popup))]
public class PopupListDto
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int Order { get; set; }

    public int Status { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }
}