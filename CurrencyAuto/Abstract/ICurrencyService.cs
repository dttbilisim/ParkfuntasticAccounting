using ecommerce.Core.Entities;
namespace CurrencyAuto.Abstract;
public interface ICurrencyService{
    Task<List<Currency>> GetTodayRatesAsync();
    Task SaveCurrenciesAsync(IEnumerable<Currency> currencies);
}
