using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;

namespace ecommerce.Admin.Domain.Dtos.CurrencyDto;

[AutoMap(typeof(Currency), ReverseMap = true)]
public class CurrencyUpsertDto
{
    public int? Id { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string CurrencyName { get; set; } = string.Empty;
    public decimal ForexBuying { get; set; }
    public decimal ForexSelling { get; set; }
    public decimal BanknoteBuying { get; set; }
    public decimal BanknoteSelling { get; set; }
    public bool IsStatic { get; set; } = false; // Merkez bankasından kur çekerken sabit kalacak mı?
    public int Status { get; set; }

    [Ignore]
    public bool StatusBool { get; set; }
}


