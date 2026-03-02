using ecommerce.Core.Entities;

namespace ecommerce.Web.Domain.Dtos.Bank;

public class BankCardListDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int BankId { get; set; }
}
