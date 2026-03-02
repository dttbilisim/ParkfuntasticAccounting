using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class Currency:AuditableEntity<int>{
    public string CurrencyCode { get; set; } = string.Empty;
    public string CurrencyName { get; set; } = string.Empty;
    public decimal ForexBuying { get; set; }
    public decimal ForexSelling { get; set; }
    public decimal BanknoteBuying { get; set; }
    public decimal BanknoteSelling { get; set; }
    public bool IsStatic { get; set; } = false; // Merkez bankasından kur çekerken sabit kalacak mı?
}
