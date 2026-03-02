using ecommerce.Admin.Services;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Interfaces;
using ecommerce.Domain.Shared.Dtos;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Plasiyer;

public partial class SalesmanMap : ComponentBase, IAsyncDisposable
{
    [Inject] protected IOnlineUserService OnlineUserService { get; set; } = default!;
    [Inject] protected ITenantProvider TenantProvider { get; set; } = default!;
    [Inject] protected AuthenticationService Security { get; set; } = default!;
    [Inject] protected IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected NotificationService NotificationService { get; set; } = default!;
    [Inject] protected TooltipService TooltipService { get; set; } = default!;

    protected List<OnlineUserDto> salesmen = new();
    protected bool isLoading = false;
    private bool mapInitialized = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadSalesmen();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Haritayı sadece veri yüklendikten ve DOM hazır olduktan sonra başlat
        if (salesmen.Any() && !mapInitialized)
        {
            mapInitialized = true;
            await InitializeMap();
        }
    }

    /// <summary>
    /// Redis'ten BranchId bazlı plasiyerleri yükle
    /// </summary>
    private async Task LoadSalesmen()
    {
        isLoading = true;
        try
        {
            var branchId = TenantProvider.GetCurrentBranchId();
            salesmen = await OnlineUserService.GetOnlineSalesmenWithLocationAsync(branchId);
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Hata", "Plasiyer konumları yüklenemedi.");
            Console.Error.WriteLine($"Plasiyer konum yükleme hatası: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }

    /// <summary>
    /// Leaflet haritasını JS interop ile başlat
    /// </summary>
    private async Task InitializeMap()
    {
        try
        {
            // Plasiyer verilerini JS'e gönderilecek formata dönüştür
            var markers = salesmen.Select(s => new
            {
                fullName = s.FullName ?? s.Username,
                username = s.Username,
                latitude = s.Latitude!.Value,
                longitude = s.Longitude!.Value,
                lastActive = FormatLastActive(s.LastActiveTime),
                application = s.Application ?? "Bilinmiyor"
            }).ToArray();

            await JSRuntime.InvokeVoidAsync("salesmanMap.init", "salesman-map", markers);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Harita başlatma hatası: {ex.Message}");
        }
    }

    /// <summary>
    /// Haritayı yenile — verileri tekrar çek ve haritayı güncelle
    /// </summary>
    protected async Task RefreshMap()
    {
        mapInitialized = false;
        await LoadSalesmen();
        
        if (salesmen.Any())
        {
            mapInitialized = true;
            StateHasChanged();
            // DOM güncellemesi sonrası haritayı yeniden başlat
            await Task.Delay(100);
            await InitializeMap();
        }
        
        NotificationService.Notify(NotificationSeverity.Info, "Güncellendi", $"{salesmen.Count} plasiyer konumu yenilendi.");
    }

    /// <summary>
    /// Haritada belirli bir plasiyere odaklan
    /// </summary>
    protected async Task FocusOnSalesman(OnlineUserDto user)
    {
        if (user.Latitude.HasValue && user.Longitude.HasValue)
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("salesmanMap.focusOn", user.Latitude.Value, user.Longitude.Value, user.Username);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Harita odaklama hatası: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Plasiyerin günlük konum geçmişini haritada rota olarak göster
    /// </summary>
    protected async Task ShowLocationHistory(OnlineUserDto user)
    {
        try
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            Console.WriteLine($"[ROTA SORGU] userId={user.UserId}, date={today}, key=location_history:{user.UserId}:{today}");
            var history = await OnlineUserService.GetLocationHistoryAsync(user.UserId, today);
            Console.WriteLine($"[ROTA SONUÇ] userId={user.UserId}, bulunan nokta sayısı={history.Count}");

            if (!history.Any())
            {
                NotificationService.Notify(NotificationSeverity.Warning, "Geçmiş Yok", $"{user.FullName ?? user.Username} için bugün konum geçmişi bulunamadı.");
                return;
            }

            // JS'e gönderilecek formata dönüştür
            var points = history.Select(h => new
            {
                lat = h.Lat,
                lng = h.Lng,
                ts = h.Ts
            }).ToArray();

            await JSRuntime.InvokeVoidAsync("salesmanMap.showRoute", user.UserId, user.FullName ?? user.Username, points);
            NotificationService.Notify(NotificationSeverity.Info, "Rota Gösteriliyor", $"{user.FullName}: {history.Count} konum noktası");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Konum geçmişi hatası: {ex.Message}");
            NotificationService.Notify(NotificationSeverity.Error, "Hata", "Konum geçmişi yüklenemedi.");
        }
    }

    /// <summary>
    /// Son aktivite zamanını okunabilir formata dönüştür
    /// </summary>
    private static string FormatLastActive(DateTime lastActiveTime)
    {
        var diff = DateTime.UtcNow - lastActiveTime;
        if (diff.TotalMinutes < 1) return "Az önce";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} dk önce";
        return $"{(int)diff.TotalHours} saat önce";
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("salesmanMap.dispose");
        }
        catch
        {
            // Sayfa kapanırken JS hatası olabilir — sessizce geç
        }
    }
}
