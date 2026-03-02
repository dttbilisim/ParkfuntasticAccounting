using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using ecommerce.Core.Dtos;
using ecommerce.Core.Entities;
using ecommerce.Core.Identity;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.Threading;
using ecommerce.Domain.Shared.Conts;
using ecommerce.Web.Domain.Dtos.Cart;
using ecommerce.Web.Domain.Services.Abstract;
using ecommerce.Web.Events;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using System.Globalization;
using System.Threading;

namespace ecommerce.Web.Utility;

public class AppStateManager(ICookieManager cookieManager, IJSRuntime jsRuntime, IHttpContextAccessor _httpContextAccessor, IConfiguration configuration, AuthenticationStateProvider _authenticationStateProvider, ICartService cartService)
{
    private CancellationTokenSource? _stateCts;
    private CancellationTokenSource? _loadingCts;

    private IJSRuntime _jsRuntime { get; set; } = jsRuntime;
    private ICookieManager _cookieManager { get; set; } = cookieManager;
    public event EventHandler<string> LanguageChanged;
    private CartDto ? CurrentCart{get;set;}
    public static IConfiguration _configuration;
    private readonly SemaphoreSlim _cartSemaphore = new(1, 1);
    public event Action<ComponentBase, string, CartDto?> StateChanged;
    private readonly ICartService _cartService = cartService;
    
    // Global Loading State
    private bool _isGlobalLoading = false;
    private readonly SemaphoreSlim _loadingSemaphore = new(1, 1);
    public event Action<bool> GlobalLoadingChanged;
    public static string ? Settings(string key){return _configuration?.GetSection(key)?.Value;}

    internal void InvokeLanguageChanged(string newLanguage, object sender = null)
        => LanguageChanged?.Invoke(sender ?? this, newLanguage);
    
    public async Task UpdatedCart(ComponentBase component, CartDto ? cart = null){
        CurrentCart = cart ?? await GetCart(true);

        // Thread-safe debounce for state change event
        var newCts = new CancellationTokenSource();
        var oldCts = Interlocked.Exchange(ref _stateCts, newCts);
        oldCts?.Cancel();
        oldCts?.Dispose();

        try {
            var token = newCts.Token;
            await Task.Delay(10, token);
            if (!token.IsCancellationRequested)
            {
                StateChanged?.Invoke(component, AppStateEvents.updateCart, CurrentCart);
            }
        } catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ UpdatedCart error: {ex.Message}");
        }
    }
    public async Task<CartDto> GetCart(){return await GetCart(false);}
    private async Task<CartDto> GetCart(bool force){
        if(!await IsAuthenticated()){
            return new CartDto();
        }
        if(CurrentCart != null && !force){
            return CurrentCart;
        }
        using(await _cartSemaphore.LockAsync()){
            if(CurrentCart != null && !force){
                return CurrentCart;
            }
            var prefs = await GetCartPreferences();
            var result = await _cartService.GetCart(prefs);
            if(!result.Ok){
                return new CartDto();
            }
            CurrentCart = result.Result;
            return CurrentCart;
        }
    }
    public Task<UserClaims> GetUserFromCookie() => Task.FromResult(new UserClaims());
    public Task<AuthenticationState> GetAuthenticationStateAsync(){
        // ensure static configuration set once
        _configuration ??= configuration;
        return _authenticationStateProvider.GetAuthenticationStateAsync();
    }
    public async Task<bool> IsAuthenticated(){
        var state = await GetAuthenticationStateAsync();
        return state.User.Identity?.IsAuthenticated == true;
    }
    public bool IsValidTurkishPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber)) return false;
        phoneNumber = phoneNumber.Trim();
        if (!Regex.IsMatch(phoneNumber, @"^\d+$")) return false;
        if (phoneNumber.StartsWith("0")) phoneNumber = phoneNumber.Substring(1);
        if (phoneNumber.Length != 10) return false;
        string[] validPrefixes = Enumerable.Range(501, 90)
            .Select(x => x.ToString())
            .ToArray();
        var prefix = phoneNumber.Substring(0, 3);
        return Array.Exists(validPrefixes, p => p == prefix);
    }

    public bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        var emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return Regex.IsMatch(email, emailPattern);
    }
    public bool IsValidChassisNumber(string chassisNumber)
    {
        if (string.IsNullOrWhiteSpace(chassisNumber)) return false;
        chassisNumber = chassisNumber.Trim();
        return chassisNumber.Length == 16;
    }
    
    public bool IsValidTCKN(string tckn)
    {
        // Uzunluk kontrolü
        if (string.IsNullOrWhiteSpace(tckn) || tckn.Length != 11)
            return false;

        // Numerik kontrol
        if (!tckn.All(char.IsDigit))
            return false;

        // İlk hane sıfır olamaz
        if (tckn[0] == '0')
            return false;

        // Rakamları int'e çevir
        int[] digits = tckn.Select(x => int.Parse(x.ToString())).ToArray();

        // 1.,3.,5.,7.,9. hanelerin toplamı (tekler)
        int sumOdd = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];

        // 2.,4.,6.,8. hanelerin toplamı (çiftler)
        int sumEven = digits[1] + digits[3] + digits[5] + digits[7];

        // 10. hane kontrolü
        int digit10 = ((sumOdd * 7) - sumEven) % 10;
        if (digit10 != digits[9])
            return false;

        // 11. hane kontrolü
        int digit11 = (digits.Take(10).Sum()) % 10;
        if (digit11 != digits[10])
            return false;

        return true;
    }
    
    public bool IsValidTaxNumber(string taxNumber)
    {
        // Vergi numarası 10 hane ve sadece rakamlardan oluşmalı
        if (string.IsNullOrWhiteSpace(taxNumber) || taxNumber.Length != 10)
            return false;

        // Numerik kontrol
        if (!taxNumber.All(char.IsDigit))
            return false;

        return true;
    }
    
    public bool CheckLogin(){
        var fullname=  _httpContextAccessor.HttpContext?.Items["FullName"]?.ToString()!;
        return string.IsNullOrWhiteSpace(fullname);
    }
    public static class Development{
        public static string Production = Settings("Development:Product");
        public static string BannerList = Settings("Development:BannerList");
        public static string BannerSubList = Settings("Development:BannerSubList");
        public static string CompanyDocuments = Settings("Development:CompanyDocuments");
        public static string InvoiceDocument = Settings("Development:InvoiceDocument");
        public static string StaticPageImages = Settings("Development:StaticPageImages");
        public static string Invoice = Settings("Development:Invoice");
        public static string APIURLDEV = "https://localhost:7135/api/";
        public static string FILEDIRECTORYDEVPATH = "/Users/sezginoztemir/Repos/Projects/NeuvoRepo/EczaPro/Web/EczaPro.Web/EczaPro.Web/Server/";
        public static string FILEDIRECTORYPROCPATH = Settings("Development:Product");
        public static string CompanyDocumentProc = Settings("Development:CompanyDocuments");
        public static string IyzicoCallBackDev = "http://localhost:5077/Payment3DCallback/";
        public static string IyzicoCallBackProc = Settings("Development:IyzicoCallBackProc");
    }
    public async Task<CartCustomerSavedPreferences> GetCartPreferences(){
        try{
            var raw = await _cookieManager.GetCookie(CartConsts.CartPreferencesStorageKey);
            if(string.IsNullOrWhiteSpace(raw)) return new CartCustomerSavedPreferences();
            return JsonConvert.DeserializeObject<CartCustomerSavedPreferences>(raw!) ?? new CartCustomerSavedPreferences();
        } catch{
            return new CartCustomerSavedPreferences();
        }
    }
    public async Task SetCartPreferences(CartCustomerSavedPreferences popupCookie){
        var raw = JsonConvert.SerializeObject(popupCookie);
        await _cookieManager.SetCookie(CartConsts.CartPreferencesStorageKey, raw, 1);
    }
    public async Task SetCookie(CookieHeaderValue cookie){await _jsRuntime.InvokeVoidAsync("setCookieRaw", cookie.ToString());}
    public async Task<CookieState ?> GetCookie(string key){
        var value = await _cookieManager.GetCookie(key);
        if(string.IsNullOrEmpty(value)) return null;
        return new CookieState(key, value);
    }
    
    // Global Loading Methods
    public bool IsGlobalLoading => _isGlobalLoading;
    
    public async Task SetGlobalLoading(bool isLoading)
    {
        using (await _loadingSemaphore.LockAsync())
        {
            if (_isGlobalLoading != isLoading)
            {
                _isGlobalLoading = isLoading;
                
                // Thread-safe debounce for loading change
                var newCts = new CancellationTokenSource();
                var oldCts = Interlocked.Exchange(ref _loadingCts, newCts);
                oldCts?.Cancel();
                oldCts?.Dispose();

                try {
                    var token = newCts.Token;
                    await Task.Delay(5, token);
                    if (!token.IsCancellationRequested)
                    {
                        GlobalLoadingChanged?.Invoke(isLoading);
                    }
                } catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ SetGlobalLoading error: {ex.Message}");
                }
            }
        }
    }
    
    public async Task<T> ExecuteWithLoading<T>(Func<Task<T>> operation, string? operationName = null)
    {
        await SetGlobalLoading(true);
        try
        {
            return await operation();
        }
        finally
        {
            await SetGlobalLoading(false);
        }
    }
    
    public async Task ExecuteWithLoading(Func<Task> operation, string? operationName = null)
    {
        await SetGlobalLoading(true);
        try
        {
            await operation();
        }
        finally
        {
            await SetGlobalLoading(false);
        }
    }

    // Text helpers
    public string ToTitleCase(string? input, string? culture = "tr-TR")
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var ci = new CultureInfo(culture ?? "tr-TR");
        var lower = input.Trim().ToLower(ci);
        return ci.TextInfo.ToTitleCase(lower);
    }

    public string ResolveImageUrl(string? pictureUrl)
    {
        if (!string.IsNullOrWhiteSpace(pictureUrl))
        {
            var trimmed = pictureUrl.Trim();
            
            // If already absolute URL, return as-is
            if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absoluteUri))
            {
                return absoluteUri.ToString();
            }

            var baseUrl = _configuration?.GetSection("Cdn")?["BaseUrl"]?.TrimEnd('/') ?? string.Empty;
            var normalizedPath = trimmed.TrimStart('/');

            if (!string.IsNullOrEmpty(baseUrl))
            {
                // Add ProductImages folder for product images
                if (normalizedPath.Contains("ProductImages/")) return $"{baseUrl}/{normalizedPath}";
                return $"{baseUrl}/ProductImages/{normalizedPath}";
            }

            return $"/assets/images/ProductImages/{normalizedPath}";
        }

        return "/assets/images/no-product-image.png";
    }

    public string GetOrderStatusClass(string status) => status?.ToLower() switch
    {
        "pending" or "beklemede" => "status-pending",
        "processing" or "işleniyor" => "status-processing",
        "shipped" or "kargoda" => "status-shipped",
        "delivered" or "teslim edildi" => "status-delivered",
        "cancelled" or "iptal" => "status-cancelled",
        _ => "status-pending"
    };

    public string GetOrderStatusIcon(string status) => status?.ToLower() switch
    {
        "pending" or "beklemede" => "ri-time-line",
        "processing" or "işleniyor" => "ri-loader-4-line",
        "shipped" or "kargoda" => "ri-truck-line",
        "delivered" or "teslim edildi" => "ri-checkbox-circle-line",
        "cancelled" or "iptal" => "ri-close-circle-line",
        _ => "ri-time-line"
    };

    public string GetOrderStatusText(string status) => status?.ToLower() switch
    {
        "pending" => "Beklemede",
        "processing" => "İşleniyor",
        "shipped" => "Kargoda",
        "delivered" => "Teslim Edildi",
        "cancelled" => "İptal Edildi",
        _ => status ?? "Bilinmiyor"
    };

    public string GetProductImageUrl(string fileName, string? fileGuid = null)
    {
        string fileToUse = !string.IsNullOrEmpty(fileGuid) ? fileGuid : fileName;

        if (string.IsNullOrEmpty(fileToUse)) return "/assets/images/no-product-image.png";
        if (fileToUse.StartsWith("http")) return fileToUse;
        var baseUrl = _configuration?.GetSection("Cdn")?["BaseUrl"]?.TrimEnd('/') ?? string.Empty;
        return $"{baseUrl}/ProductImages/{fileToUse}";
    }

    public string GetProductImageUrl(IEnumerable<ecommerce.Domain.Shared.Dtos.Product.ProductImageDto>? images)
    {
        var firstImage = images?.OrderBy(x => x.Order).FirstOrDefault();
        if (firstImage == null) return "/assets/images/no-product-image.png";

        return GetProductImageUrl(firstImage.FileName, firstImage.FileGuid);
    }

    public string GetImageFallbackScript()
    {
        return "this.onerror=null; this.src='/assets/images/no-product-image.png';";
    }
}