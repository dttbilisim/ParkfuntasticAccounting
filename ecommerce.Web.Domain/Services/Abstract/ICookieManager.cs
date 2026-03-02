namespace ecommerce.Web.Domain.Services.Abstract;

public interface ICookieManager{
    Task SetCookie(string key, string value, int day);
    Task<string> GetCookie(string key);
    Task DeleteCookie(string key);
    Task<bool> IsUserLoggedIn();
    
    Task<string> GetFullNAme();
    Task<int> GetUserId();
}
