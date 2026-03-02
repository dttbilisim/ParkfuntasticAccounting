using CurrencyAuto.Abstract;
using ecommerce.Core.BackgroundJobs;
using Microsoft.Extensions.Logging;
namespace CurrencyAuto.BackgroungServices;
public class CurrencyBackgroundService(ICurrencyService _currencyService, ILogger<CurrencyBackgroundService> _logger) : IAsyncBackgroundJob{
    public async Task ExecuteAsync(){
        try{
            var currencies = await _currencyService.GetTodayRatesAsync();
            if(currencies != null){
                if(!currencies.Any()){
                    _logger.LogWarning("CurrencyBackgroundService: No currencies returned from GetTodayRatesAsync at {Time}", DateTime.UtcNow);
                    return;
                }
                _logger.LogInformation("CurrencyBackgroundService: Retrieved {Count} currency rate(s) at {Time}", currencies.Count, DateTime.UtcNow);
                try{
                    await _currencyService.SaveCurrenciesAsync(currencies);
                    _logger.LogInformation("CurrencyBackgroundService: Successfully saved {Count} currency rate(s) at {Time}", currencies.Count, DateTime.UtcNow);
                } catch(Exception ex){
                    _logger.LogError(ex, "CurrencyBackgroundService: Error while saving currencies at {Time}", DateTime.UtcNow);
                    throw;
                }
            }
        } catch(Exception e){
            _logger.LogError(e, "CurrencyBackgroundService: Unhandled exception at {Time}", DateTime.UtcNow);
            throw;
        }
    }
}
