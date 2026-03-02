using AutoMapper;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.CurrencyDto;

[AutoMap(typeof(Currency))]
public class CurrencyListDto
{
    public int Id { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string CurrencyName { get; set; } = string.Empty;
    public decimal ForexBuying { get; set; }
    public decimal ForexSelling { get; set; }
    public decimal BanknoteBuying { get; set; }
    public decimal BanknoteSelling { get; set; }
    public DateTime CreatedDate { get; set; }
    public EntityStatus Status { get; set; }
}


