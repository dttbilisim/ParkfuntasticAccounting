using ecommerce.Core.Entities;

namespace ecommerce.Web.Domain.Dtos.Bank;

public class BankListDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string LogoPath { get; set; }
    public string SystemName { get; set; }
}
