using System.Xml.Linq;
using CurrencyAuto.Abstract;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.EFCore.Context;
using Microsoft.EntityFrameworkCore;
namespace CurrencyAuto.Concreate;
public class CurrencyService(HttpClient _httpClient, IUnitOfWork<ApplicationDbContext> _context) : ICurrencyService{
    public async Task<List<Currency>> GetTodayRatesAsync(){
        var url = "https://www.tcmb.gov.tr/kurlar/today.xml";
        var xml = await _httpClient.GetStringAsync(url);
        var xDoc = XDocument.Parse(xml);
        var list = new List<Currency>();
        foreach(var currency in xDoc.Descendants("Currency")){
            var rate = new Currency{
                CurrencyCode = currency.Attribute("CurrencyCode")?.Value ?? "",
                CurrencyName = currency.Element("Isim")?.Value ?? "",
                ForexBuying = decimal.TryParse(currency.Element("ForexBuying")?.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var fb) ? fb : 0,
                ForexSelling = decimal.TryParse(currency.Element("ForexSelling")?.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var fs) ? fs : 0,
                BanknoteBuying = decimal.TryParse(currency.Element("BanknoteBuying")?.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var bb) ? bb : 0,
                BanknoteSelling = decimal.TryParse(currency.Element("BanknoteSelling")?.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var bs) ? bs : 0,
            };
            list.Add(rate);
        }
        return list;
    }
    public async Task SaveCurrenciesAsync(IEnumerable<Currency> currencies){
        foreach(var currency in currencies){
            // Check if the latest record for this currency is marked as static
            var lastRecord = await _context.DbContext.Currencies
                .Where(x => x.CurrencyCode == currency.CurrencyCode)
                .OrderByDescending(x => x.CreatedDate)
                .FirstOrDefaultAsync();

            if (lastRecord != null && lastRecord.IsStatic)
            {
                continue; // Skip updating/adding if static
            }

            currency.CreatedDate = DateTime.Now;
            currency.CreatedId = 1;
            currency.Status = 1;

            // Eğer aynı gün aynı kur varsa tekrar eklememek için kontrol
            var exists = await _context.DbContext.Currencies.AnyAsync(x => x.CurrencyCode == currency.CurrencyCode && x.CreatedDate.Date == DateTime.Now.Date);
            if(!exists) _context.DbContext.Currencies.Add(currency);
        }
        await _context.DbContext.SaveChangesAsync();
    }
}
