using AutoMapper;
using Newtonsoft.Json;
using ecommerce.Admin.Domain.Dtos.OrderDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Entities;
using Microsoft.AspNetCore.Components;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.JSInterop;
using Radzen;
using Microsoft.AspNetCore.Components.Web;
using ecommerce.Core.Models;
using Radzen.Blazor;
using ecommerce.Admin.Domain.Dtos.MembershipDto;
using ecommerce.Admin.Domain.Dtos.CompanyDto;
using ecommerce.Core.Extensions;
using ecommerce.Core.Helpers;
using ecommerce.Core.Utils;
using Microsoft.AspNetCore.Components.Forms;
using ecommerce.Admin.Domain.Dtos.SellerAddressDto;
namespace ecommerce.Admin.Components.Pages.Modals{
    public partial class OrderDetail : IDisposable {
        public class OrderItemViewModel
        {
            public OrderItems Item { get; set; }
            public string AssignedWH { get; set; } = "Diğer";
        }
        [Inject] protected IJSRuntime JSRuntime{get;set;} = null!;
        [Inject] protected NavigationManager NavigationManager{get;set;} = null!;
        [Inject] protected DialogService DialogService{get;set;} = null!;
        [Inject] protected TooltipService TooltipService{get;set;} = null!;
        [Inject] protected ContextMenuService ContextMenuService{get;set;} = null!;
        [Inject] protected NotificationService NotificationService{get;set;} = null!;
        [Inject] protected IOrderService OrderService{get;set;} = null!;
        [Inject] protected ICargoCreationService CargoCreationService{get;set;} = null!;
        [Inject] public IMapper _mapper{get;set;} = null!;
        [Inject] protected AuthenticationService Security{get;set;} = null!;
        [Inject] public ICityService CityService{get;set;} = null!;
        [Inject] public ITownService TownService{get;set;} = null!;
        [Inject] public ISellerAddressService SellerAddressService{get;set;} = null!;
        [Inject] public ecommerce.Domain.Shared.Abstract.IOtoIsmailService OtoIsmailService{get;set;} = null!;
        [Inject] public Dega.Abstract.IDegaService DegaService{get;set;} = null!;
        [Inject] public Remar.Abstract.IRemarApiService RemarApiService{get;set;} = null!;
        [Inject] public ecommerce.Odaksodt.Abstract.IOdaksoftInvoiceService OdaksoftInvoiceService{get;set;} = null!;
        [Parameter] public int Id{get;set;}
        protected bool IsShowLoadingBar = true;
        private OrderListDto order = null!;
        private List<OrderInvoiceListDto> _orderInvoiceList = new();
        protected bool errorVisible;
        int count;
        protected long MaxFileSize = 1024 * 1024 * 2;
        protected int maxAllowedFiles = 5;
        [Inject] public IConfiguration Configuration{get;set;} = null!;
        protected RadzenDataGrid<OrderItemViewModel> ? radzenDataGrid = new();
        protected List<CityListDto> Sellercities = new();
        protected List<TownListDto> Sellertowns = new();
        protected List<CityListDto> Buyercities = new();
        protected List<TownListDto> Buyertowns = new();
        protected CompanyUpsertDto ? SellerCompany{get;set;} = new();
        protected CompanyUpsertDto ? BuyerCompany{get;set;} = new();
        protected RadzenDataGrid<OrderInvoiceListDto> ? radzenDataGridInvoice = new();
        IEnumerable<InvoiceType> InvoiceType = Enum.GetValues(typeof(InvoiceType)).Cast<InvoiceType>();
        private OrderInvoiceUpsertDto InvoiceUpsertDto = new();
        private string BaseUrl = null!;
        protected string FullAddress = "";
        protected List<OrderItemViewModel> orderitemsGrouped = new();
        protected Dictionary<int, string> ItemStocks = new();
        protected List<SellerAddressListDto> SellerAddresses = new();
        private bool _isOrderLoading = false;
        protected bool _isCheckingStocks = false;

        protected bool _isStocksChecked = false;
        protected bool _isAddingToCart = false;
        protected bool _isDownloadingEInvoice = false;
        protected bool IsSupplierOrderCreated => order?.OrderItems?.Any() == true && order.OrderItems.All(x => x.IsSellerOrderStatus == true);

        private System.Threading.SemaphoreSlim _semaphore = new(1, 1);
        private bool _disposed;

        public void Dispose()
        {
            _disposed = true;
            _semaphore?.Dispose();
        }


        protected override async Task OnInitializedAsync(){
            BaseUrl = Configuration.GetValue<string>("FileUrl") + "OrderInvoice";
            
            // Load Cities early
            var citiesResponse = await CityService.GetCities();
            if(citiesResponse.Ok){
                Sellercities = citiesResponse.Result;
                Buyercities = citiesResponse.Result;
            }

            await LoadOrderDetails();
            await GetOrderInvoiceList(Id);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && radzenDataGrid != null)
            {
                // Enable grouping programmatically
                radzenDataGrid.Groups.Add(new Radzen.GroupDescriptor { Property = "AssignedWH", Title = "Depo" });
                await radzenDataGrid.Reload();
            }
        }

        private async Task LoadOrderDetails()
        {
            if (_isOrderLoading || _disposed) return;
            try 
            {
                await _semaphore.WaitAsync();
                if (_disposed) return;

                _isOrderLoading = true;
                var response = await OrderService.GetOrderDetailById(Id);
                if(response.Ok && response.Result != null)
                {
                    order = response.Result;
                    orderitemsGrouped = order.OrderItems?.Select(x => new OrderItemViewModel { Item = x }).ToList() ?? new();
                    count = orderitemsGrouped.Count;
                    InvoiceUpsertDto.InvoiceDate = DateTime.Now;

                    // Seller Mapping
                    if(order.Seller != null)
                    {
                        SellerCompany = _mapper.Map<CompanyUpsertDto>(order.Seller);
                        SellerCompany.Address = order.Seller.Address;
                        SellerCompany.PhoneNumber = order.Seller.PhoneNumber;
                        SellerCompany.EmailAddress = order.Seller.Email;
                        
                        if(SellerCompany.CityId > 0)
                        {
                            var townsResponse = await TownService.GetTownsByCityId(SellerCompany.CityId);
                            if(townsResponse.Ok) Sellertowns = townsResponse.Result;
                        }
                    }

                    // Buyer Mapping
                    if(order.UserAddress != null)
                    {
                        BuyerCompany = new CompanyUpsertDto {
                            FirstName = !string.IsNullOrWhiteSpace(order.UserAddress.FullName) ? order.UserAddress.FullName : 
                                        (order.CustomerName ?? ""), // Use CustomerName (set via helper properties)
                            LastName = "",
                            CityId = order.UserAddress.CityId ?? 0,
                            TownId = order.UserAddress.TownId ?? 0,
                            Address = order.UserAddress.Address ?? "",
                            EmailAddress = order.UserAddress.Email ?? "",
                            PhoneNumber = order.UserAddress.PhoneNumber ?? ""
                        };

                        if(BuyerCompany.CityId > 0)
                        {
                            var townsResponse = await TownService.GetTownsByCityId(BuyerCompany.CityId);
                            if(townsResponse.Ok) Buyertowns = townsResponse.Result;
                        }
                    } 
                    else if(!string.IsNullOrWhiteSpace(order.CustomerName)) 
                    {
                        // In Admin context, Company is null, use CustomerName instead
                        BuyerCompany = new CompanyUpsertDto {
                            FirstName = order.CustomerName,
                            LastName = "",
                            EmailAddress = "", // Not available in OrderListDto
                            PhoneNumber = "" // Not available in OrderListDto
                        };
                    }


                    // CustomerName Fallback
                    if (string.IsNullOrWhiteSpace(order.CustomerName))
                    {
                        order.CustomerName = !string.IsNullOrWhiteSpace(BuyerCompany?.FirstName) ? BuyerCompany.FirstName : "";
                    }

                    // Construct FullAddress
                    UpdateFullAddress();
                    
                    // Construct FullAddress
                    UpdateFullAddress();
                    
                    // Load Seller Addresses for Warehouse Mapping
                    if (order.Seller?.Id > 0)
                    {
                        var addrResponse = await SellerAddressService.GetSellerAddresses(order.Seller.Id);
                        if (addrResponse.Ok)
                        {
                            SellerAddresses = addrResponse.Result;
                        }
                    }

                    // RESTORE ASSIGNMENTS if we already checked stocks
                    if (ItemStocks != null && ItemStocks.Any())
                    {
                        UpdateItemAssignments(ItemStocks);
                    }

                    if (!_disposed)
                    {
                        StateHasChanged();
                    }
                }
                else if (!response.Ok)
                {
                    errorVisible = true;
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                }
            }
            catch (ObjectDisposedException) { /* Safe to ignore */ }
            finally 
            {
                _isOrderLoading = false;
                if (!_disposed)
                {
                    try { _semaphore.Release(); } catch { }
                }
            }
        }



        protected async Task CheckStocks()
        {
            if (_disposed) return;
            
            // Eğer işlem zaten devam ediyorsa, kullanıcıya tekrar bastırmayalım ama işlemi de kesmeyelim.
            // Ancak kullanıcı "çalışmıyor" diyorsa, belki de bu flag önceki bir işlemden dolayı takılı kalmıştır.
            // Bu nedenle basit bir logic hatası olabilir.
            if (_isCheckingStocks) 
            {
                // İşlem zaten sürüyor, tekrar başlatma.
                return;
            }

            bool semaphoreAcquired = false;
            try
            {
                _isCheckingStocks = true;
                if (!_disposed) StateHasChanged();
                
                // Kısa bir bekleme ile UI'ın güncellenmesini sağla (spinleri vs göstermek için)
                await Task.Delay(50);
                
                if (_disposed) return;
                
                // Semaphore ile aynı anda sadece tek bir stok kontrolü çalışsın
                await _semaphore.WaitAsync();
                semaphoreAcquired = true;
                
                if (_disposed) return;

                var stocks = await OrderService.GetOrderItemWarehouseStocks(Id);
                
                if (stocks != null && orderitemsGrouped.Any())
                {
                    UpdateItemAssignments(stocks);
                }
            }
            catch (ObjectDisposedException)
            {
                // Ignored
            }
            catch (Exception ex)
            {
                if (!_disposed)
                    NotificationService.Notify(NotificationSeverity.Error, "Stok kontrolü sırasında hata oluştu", ex.Message);
            }
            finally
            {
                _isCheckingStocks = false;
                _isStocksChecked = true;
                if (semaphoreAcquired && !_disposed)
                {
                    try { _semaphore.Release(); } catch { }
                }
                
                if (!_disposed)
                {
                    StateHasChanged();
                    // Task.Yield yerine Task.Delay(1) UI'a nefes aldırır ama geri dönüşü daha garantilidir
                    await Task.Delay(1);
                    
                    if (radzenDataGrid != null)
                    {
                        await radzenDataGrid.Reload();
                    }
                }
            }
        }

        private void UpdateItemAssignments(Dictionary<int, string> newStocks)
        {
            if (newStocks != null)
            {
                foreach (var kvp in newStocks)
                {
                    ItemStocks[kvp.Key] = kvp.Value;
                }
            }

            if (orderitemsGrouped == null || !orderitemsGrouped.Any()) return;

            string Normalize(string input)
            {
                if (string.IsNullOrEmpty(input)) return input;
                return input.Trim()
                    .Replace("İ", "I")
                    .Replace("i", "I")
                    .Replace("ı", "I")
                    .ToUpperInvariant();
            }

            // 1. Analiz: Hangi depo ürünün **TAM MİKTARINI** karşılıyor? (Consolidation + Quantity Check)
            var warehouseCoverage = new Dictionary<string, int>();

            foreach (var vm in orderitemsGrouped)
            {
                if (!ItemStocks.TryGetValue(vm.Item.Id, out var stockInfo) || string.IsNullOrEmpty(stockInfo) || stockInfo == "Stok Yok") 
                   continue;

                var warehouses = stockInfo.Split(new[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var wh in warehouses)
                {
                    var parts = wh.Split(':');
                    if (parts.Length < 1) continue;

                    var whName = parts[0].Trim();
                    int stock = 0;
                    if (parts.Length > 1)
                    {
                         var valStr = parts[1].Trim();
                         if (string.Equals(valStr, "VAR", StringComparison.OrdinalIgnoreCase)) stock = 100;
                         else int.TryParse(valStr, out stock);
                    }

                    // KRİTİK KURAL: Talep edilen adedi karşılıyor mu?
                    if (stock >= vm.Item.Quantity)
                    {
                        var normWhName = Normalize(whName);
                        if (!warehouseCoverage.ContainsKey(normWhName)) warehouseCoverage[normWhName] = 0;
                        warehouseCoverage[normWhName]++;
                    }
                }
            }

            // 2. Ana Depo Belirle
            string primaryWarehouse = warehouseCoverage.OrderByDescending(x => x.Value).FirstOrDefault().Key;

            // Track usage (for display grouping later)
            var usedWarehouses = new Dictionary<string, int>();
            
            foreach (var vm in orderitemsGrouped)
            {
                vm.AssignedWH = "Diğer"; // Default

                if (ItemStocks.TryGetValue(vm.Item.Id, out var stockInfo) && !string.IsNullOrEmpty(stockInfo) && stockInfo != "Stok Yok")
                {
                     var warehouses = stockInfo.Split(new[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries);
                     var validWarehouses = new List<(string Name, int Stock)>();
                     
                     foreach (var wh in warehouses)
                     {
                         var parts = wh.Split(':');
                         if (parts.Length < 1) continue;
                         
                         var whName = parts[0].Trim();
                         // Plaza deposu iptal
                         if (whName.Equals("P", StringComparison.OrdinalIgnoreCase) || 
                             whName.Equals("Plaza", StringComparison.OrdinalIgnoreCase))
                         {
                             continue;
                         }

                         int stock = 0;
                         if (parts.Length > 1)
                         {
                             var valStr = parts[1].Trim();
                             if (valStr.Equals("VAR", StringComparison.OrdinalIgnoreCase)) stock = 100;
                             else int.TryParse(valStr, out stock);
                         }

                         validWarehouses.Add((whName, stock));
                     }
                     if (validWarehouses.Any())
                     {
                        var selected = validWarehouses
                            .OrderByDescending(w => !string.IsNullOrEmpty(primaryWarehouse) && Normalize(w.Name) == primaryWarehouse ? 1000 : 0)
                            .ThenByDescending(w => usedWarehouses.ContainsKey(Normalize(w.Name)) ? usedWarehouses[Normalize(w.Name)] : 0)
                            .ThenByDescending(w => w.Stock)
                            .FirstOrDefault();
                        
                         // RESOLVE FRIENDLY NAME FROM SELLER ADDRESSES
                         var normSelected = Normalize(selected.Name);
                         var matchedAddress = SellerAddresses?.FirstOrDefault(a => 
                             !string.IsNullOrEmpty(a.StockWhereIs) && 
                             a.StockWhereIs.Split(new[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Any(s => Normalize(s) == normSelected));

                         vm.AssignedWH = matchedAddress != null && !string.IsNullOrEmpty(matchedAddress.Title) 
                                         ? matchedAddress.Title 
                                         : selected.Name + " (Tanımsız)";

                         var groupKey = Normalize(vm.AssignedWH);
                         if (!usedWarehouses.ContainsKey(groupKey))
                             usedWarehouses[groupKey] = 0;
                         usedWarehouses[groupKey]++;
                     }
                     else if (stockInfo.Contains("VAR", StringComparison.OrdinalIgnoreCase))
                     {
                         vm.AssignedWH = "API'de Mevcut";
                     }
                }
                else
                {
                    vm.AssignedWH = "Stok Yok";
                }
            }

            // Sort by assigned warehouse for visual grouping
            orderitemsGrouped = orderitemsGrouped.OrderBy(x => x.AssignedWH).ToList();
        }

        private void UpdateFullAddress()
        {
            if (BuyerCompany != null)
            {
                var cityName = Buyercities?.FirstOrDefault(x => x.Id == BuyerCompany.CityId)?.Name;
                var townName = Buyertowns?.FirstOrDefault(x => x.Id == BuyerCompany.TownId)?.Name;
                FullAddress = $"{BuyerCompany.Address} {(string.IsNullOrEmpty(cityName) ? "" : cityName)}{(string.IsNullOrEmpty(townName) ? "" : "/" + townName)}".Trim();
            }
        }

        protected SellerAddressListDto GetSellerAddressByWH(string whName)
        {
            if (string.IsNullOrEmpty(whName) || SellerAddresses == null || !SellerAddresses.Any()) 
                return null;

            string Normalize(string input)
            {
                if (string.IsNullOrEmpty(input)) return input;
                return input.Trim()
                    .Replace("İ", "I")
                    .Replace("i", "I")
                    .Replace("ı", "I")
                    .ToUpperInvariant();
            }

            var normalizedWh = Normalize(whName);

            return SellerAddresses.FirstOrDefault(a => 
                !string.IsNullOrEmpty(a.StockWhereIs) && 
                a.StockWhereIs.Split(new[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries)
                    .Any(s => Normalize(s) == normalizedWh));
        }

        
        private async Task LoadData(LoadDataArgs args){
            await LoadOrderDetails();
        }
        protected void CancelButtonClick(MouseEventArgs args){DialogService.Close(null);}
        protected void ChangeInvoiceType(object value){InvoiceUpsertDto.InvoiceType = (InvoiceType) value;}
        protected async Task LoadFiles(InputFileChangeEventArgs e, string additionalParameter = ""){
            var directoryName = "";
            var oldFileName = "";
            if(!string.IsNullOrEmpty(additionalParameter)){
                directoryName = additionalParameter;
            }
            foreach(var item in e.GetMultipleFiles(maxAllowedFiles)){
                try{
                    var newFileName = await PrepareUniqueImageName(item);
                    if(!string.IsNullOrEmpty(directoryName)){
                        oldFileName = newFileName;
                        newFileName = directoryName+@"\"+newFileName;
                       
                    }
                    var directoryPath = await GetDirectoryPathByKey("UploadImagePath", directoryName);
                    await DirectoryControl(directoryPath);
                    var path = "";
                    path = await GetDirectoryPathByKey("UploadImagePath", newFileName);
                    await using FileStream fs = new(path, FileMode.Create);
                    await item.OpenReadStream(MaxFileSize).CopyToAsync(fs);
                    var fileUrl = await GetDirectoryPathByKey("UploadImagePath", newFileName);
                    InvoiceUpsertDto.InvoicePath = fileUrl;
                    InvoiceUpsertDto.FileName = oldFileName;
                } catch(Exception ex){
                    NotificationService.Notify(NotificationSeverity.Error, ex.Message);
                }
            }
        }
        private async Task DirectoryControl(string path = ""){
            var directoryPath = Path.Combine(Configuration.GetValue<string>("UploadImagePath")!);
            if(!string.IsNullOrEmpty(path)) directoryPath = path;
            if(!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
        }
        private async Task<string> GetDirectoryPathByKey(string key, string newFileName){
            var path = Configuration.GetValue<string>(key)!+@"\"+newFileName;
            return path;
        }
        private async Task<string> PrepareUniqueImageName(IBrowserFile item){
            var randomName = Path.GetRandomFileName();
            var extension = Path.GetExtension(item.Name);
            var newFileName = Path.ChangeExtension(randomName, extension);
            return newFileName;
        }
        private async Task SaveInvoice(){
            if(InvoiceUpsertDto.InvoiceType == 0){
                NotificationService.Notify(NotificationSeverity.Error, "Fatura Tipi Seçiniz");
            } else
                if(InvoiceUpsertDto.InvoiceDate == null){
                    NotificationService.Notify(NotificationSeverity.Error, "Fatura Tarihi Seçiniz");
                } else
                    if(string.IsNullOrWhiteSpace(InvoiceUpsertDto.InvoicePath)){
                        NotificationService.Notify(NotificationSeverity.Error, "Fatura Yükleyin");
                    } else{
                        InvoiceUpsertDto.OrderId = Id;
                        // CompanyId from Orders.CompanyId (works for both User and ApplicationUser)
                        InvoiceUpsertDto.CompanyId = order.CompanyId; // Use CompanyId from Orders (works for both User and ApplicationUser)
                        var rs = await OrderService.UpsertOrderInvoice(InvoiceUpsertDto);
                        if(rs){
                            await GetOrderInvoiceList(Id);
                            NotificationService.Notify(NotificationSeverity.Success, "Fatura Yüklendi");
                        }
                    }
        }
        private async Task GetOrderInvoiceList(int orderId){
            try{
                var rs = await OrderService.GetOrderInvoiceList(orderId);
                if(rs.Ok){
                    _orderInvoiceList = rs.Result;
                    StateHasChanged();
                }
            } catch(Exception e){
                Console.WriteLine(e);
                NotificationService.Notify(NotificationSeverity.Error, e.Message);
            }
        }
        private async Task DeleteInvoice(int invoiceId){
            try{
                var rs = await OrderService.DeleteOrderInvoice(invoiceId);
                if(rs){
                    NotificationService.Notify(NotificationSeverity.Success, "Fatura Silindi");
                    await GetOrderInvoiceList(Id);
                }
            } catch(Exception e){
                Console.WriteLine(e);
                NotificationService.Notify(NotificationSeverity.Error, e.Message);
            }
        }

        private async Task UpdateOrderStatus(OrderStatusType newStatus)
        {
            // if (order.OrderStatusType != OrderStatusType.OrderProblem)
            // {
            //     return;
            // }

            if (await DialogService.Confirm(
                    $"Sipariş durumu {newStatus.GetDisplayName()} olarak güncellenecektir. Onaylıyor musunuz?",
                    "Sipariş Durumu Güncelle",
                    new ConfirmOptions
                    {
                        OkButtonText = "Evet",
                        CancelButtonText = "Hayır"
                    }
                ) != true)
            {
                return;
            }

            var response = await OrderService.UpdateOrderStatus(
                new AuditWrapDto<OrderStatusUpdateDto>
                {
                    Dto = new OrderStatusUpdateDto
                    {
                        Id = order.Id,
                        OrderStatusType = newStatus
                    }
                }
            );

            if (!response.Ok)
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                return;
            }

            order.OrderStatusType = newStatus;
            
            NotificationService.Notify(NotificationSeverity.Success, "Sipariş durumu güncellendi");
        }

        protected bool IsCreatingCargo { get; set; } = false;

        protected async Task CreateCargoForGroup(string warehouseName, IEnumerable<OrderItemViewModel> groupItems)
        {
            if (IsCreatingCargo)
            {
                NotificationService.Notify(NotificationSeverity.Warning, "Kargo oluşturma işlemi devam ediyor, lütfen bekleyin");
                return;
            }

            // Get cargo provider name
            var cargoProviderName = order?.Cargo?.Name ?? "Bilinmeyen Kargo";
            var itemCount = groupItems.Count();

            // Show confirmation dialog
            var confirmResult = await DialogService.Confirm(
                $"{warehouseName} deposundaki {itemCount} ürün için {cargoProviderName} ile kargo oluşturulacak. Onaylıyor musunuz?",
                "Kargo Oluşturma Onayı",
                new ConfirmOptions { OkButtonText = "Evet, Oluştur", CancelButtonText = "İptal" }
            );

            if (confirmResult != true)
            {
                return; // User cancelled
            }

            try
            {
                IsCreatingCargo = true;
                StateHasChanged();

                var productIds = groupItems.Select(x => x.Item.Id).ToList();

                Console.WriteLine($"🚀 Kargo oluşturuluyor: {warehouseName} deposu, {productIds.Count} ürün, Kargo: {cargoProviderName}");

                var result = await CargoCreationService.CreateCargoForWarehouseGroupAsync(Id, warehouseName, productIds);

                if (result.Success)
                {
                    NotificationService.Notify(
                        NotificationSeverity.Success, 
                        $"{result.CargoProvider} - {warehouseName}",
                        $"Kargo başarıyla oluşturuldu. Takip No: {result.CargoTrackingNumber}"
                    );
                    Console.WriteLine($"✅ Başarılı: {result.Message} - Takip No: {result.CargoTrackingNumber}");
                    
                    // Refresh UI to show updated cargo status
                    await LoadData(null);
                }
                else
                {
                    NotificationService.Notify(
                        NotificationSeverity.Error,
                        "Kargo oluşturulamadı",
                        result.Message
                    );
                    Console.WriteLine($"❌ Hata: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Beklenmeyen hata", ex.Message);
                Console.WriteLine($"💥 Exception: {ex.Message}");
            }
            finally
            {
                IsCreatingCargo = false;
                StateHasChanged();
            }
        }

        protected bool IsCancellingCargo { get; set; } = false;

        protected async Task CancelCargoForGroup(string warehouseName, IEnumerable<OrderItemViewModel> groupItems)
        {
            if (IsCancellingCargo)
            {
                NotificationService.Notify(NotificationSeverity.Warning, "İptal işlemi devam ediyor");
                return;
            }

            var itemCount = groupItems.Count();
            var confirmResult = await DialogService.Confirm(
                $"{warehouseName} deposundaki {itemCount} ürün için Sendeo kargosu iptal edilecek. Onaylıyor musunuz?",
                "Kargo İptal Onayı",
                new ConfirmOptions { OkButtonText = "Evet, İptal Et", CancelButtonText = "Vazgeç" }
            );

            if (confirmResult != true) return;

            try
            {
                IsCancellingCargo = true;
                StateHasChanged();

                var productIds = groupItems.Select(x => x.Item.Id).ToList();
                var result = await CargoCreationService.CancelSendeoCargoAsync(Id, warehouseName, productIds);

                if (result.Success)
                {
                    NotificationService.Notify(
                        NotificationSeverity.Success,
                        "Kargo iptal edildi",
                        result.Message
                    );

                    // Refresh order items to show updated status
                    await LoadData(null);
                }
                else
                {
                    NotificationService.Notify(
                        NotificationSeverity.Error,
                        "İptal başarısız",
                        result.Message
                    );
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Hata", ex.Message);
            }
            finally
            {
                IsCancellingCargo = false;
                StateHasChanged();
            }
        }

        protected string GetAddToCartButtonText()
        {
            return "Sipariş Oluştur";
        }

        protected bool CanAddToCart()
        {
            // Sadece OtoIsmail (1), Dega (3), Remar (4) için sepete ekleme destekleniyor
            return order?.Seller?.Id is 1 or 3 or 4;
        }

        protected async Task AddToSellerCart()
        {
            if (_isAddingToCart) return;
            if (!CanAddToCart())
            {
                NotificationService.Notify(NotificationSeverity.Warning, "Bu satıcı için sepete ekleme desteklenmiyor!");
                return;
            }

            try
            {
                _isAddingToCart = true;
                StateHasChanged();

                var sellerId = order.Seller.Id;
                var itemCount = 0;
                bool success = false;
                string message = "";

                if (sellerId == 1) // OtoIsmail
                {
                    var cartItems = new List<ecommerce.Domain.Shared.Dtos.OtoIsmail.OtoIsmailCartItemDto>();
                    foreach (var item in orderitemsGrouped)
                    {
                        if (!string.IsNullOrEmpty(item.Item.SourceId) && int.TryParse(item.Item.SourceId, out int sourceId))
                        {
                            var productCode = await OrderService.GetProductCodeForCart(sellerId, sourceId);
                            if (!string.IsNullOrEmpty(productCode))
                            {
                                cartItems.Add(new ecommerce.Domain.Shared.Dtos.OtoIsmail.OtoIsmailCartItemDto { StokKodu = productCode, Adet = item.Item.Quantity });
                            }
                        }
                    }
                    if (!cartItems.Any()) { NotificationService.Notify(NotificationSeverity.Warning, "Sepete eklenecek ürün bulunamadı"); return; }
                    var result = await OtoIsmailService.AddToCartAsync(cartItems);
                    success = result?.Success == true;
                    message = result?.Message?.ToString() ?? (success ? "Ürünler sepete eklendi" : "Bilinmeyen hata");
                    itemCount = cartItems.Count;
                }
                else if (sellerId == 3) // Dega
                {
                    var cartItems = new List<Dega.Dtos.DegaCartItemDto>();
                    foreach (var item in orderitemsGrouped)
                    {
                        if (!string.IsNullOrEmpty(item.Item.SourceId) && int.TryParse(item.Item.SourceId, out int sourceId))
                        {
                            var productCode = await OrderService.GetProductCodeForCart(sellerId, sourceId);
                            if (!string.IsNullOrEmpty(productCode))
                            {
                                cartItems.Add(new Dega.Dtos.DegaCartItemDto { ProductCode = productCode, Quantity = item.Item.Quantity });
                            }
                        }
                    }
                    if (!cartItems.Any()) { NotificationService.Notify(NotificationSeverity.Warning, "Sepete eklenecek ürün bulunamadı"); return; }
                    var result = await DegaService.AddToCartAsync(cartItems);
                    success = result?.status == "succeeded";
                    message = result?.message?.Items?.FirstOrDefault()?.Msg ?? (success ? "Ürünler sepete eklendi" : "Bilinmeyen hata");
                    itemCount = cartItems.Count;
                }
                else if (sellerId == 4) // Remar
                {
                    var cartItems = new List<Remar.Dtos.RemarCartItemDto>();
                    foreach (var item in orderitemsGrouped)
                    {
                        if (!string.IsNullOrEmpty(item.Item.SourceId) && int.TryParse(item.Item.SourceId, out int sourceId))
                        {
                            var productCode = await OrderService.GetProductCodeForCart(sellerId, sourceId);
                            if (!string.IsNullOrEmpty(productCode))
                            {
                                cartItems.Add(new Remar.Dtos.RemarCartItemDto { ProductCode = productCode, Quantity = item.Item.Quantity });
                            }
                        }
                    }
                    if (!cartItems.Any()) { NotificationService.Notify(NotificationSeverity.Warning, "Sepete eklenecek ürün bulunamadı"); return; }
                    var result = await RemarApiService.AddToCartAsync(cartItems);
                    success = result?.status == "succeeded";
                    message = result?.message?.Items?.FirstOrDefault()?.Msg ?? (success ? "Ürünler sepete eklendi" : "Bilinmeyen hata");
                    itemCount = cartItems.Count;
                }

                if (success)
                {
                    NotificationService.Notify(NotificationSeverity.Success, $"Başarılı! ({itemCount} ürün gönderildi)", message);
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, "Sepete eklenemedi", message);
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Sepete eklenirken hata oluştu", ex.Message);
            }
            finally
            {
                _isAddingToCart = false;
                StateHasChanged();
            }
        }



        protected async Task CreateSupplierOrder()
        {
            if (_isAddingToCart) return;
            var sellerId = order?.Seller?.Id;

            // Dega (3), Remar (4), OtoIsmail (1) support this flow
            if (sellerId != 3 && sellerId != 4 && sellerId != 1)
            {
                 NotificationService.Notify(NotificationSeverity.Warning, "Bu işlem sadece Dega, Remar ve OtoIsmail tedarikçileri için geçerlidir.");
                 return;
            }

            if (await DialogService.Confirm("Tedarikçi sisteminde sipariş oluşturulacak. Devam etmek istiyor musunuz?", "Onay") != true)
            {
                return;
            }

            try
            {
                _isAddingToCart = true;
                StateHasChanged();

                if (sellerId == 1) // OtoIsmail Logic
                {
                    // 1. Get Cari List
                    var cariResponse = await OtoIsmailService.GetCariListesiAsync();
                    if (cariResponse == null || cariResponse.Data == null || !cariResponse.Data.Any())
                    {
                        NotificationService.Notify(NotificationSeverity.Warning, "OtoIsmail teslimat carisi listesi alınamadı veya boş.");
                        _isAddingToCart = false; 
                        return;
                    }

                    // 2. Show Dialog
                    var dialogResult = await DialogService.OpenAsync<OtoIsmailOrderDialog>("OtoIsmail Sipariş Detayları", 
                        new Dictionary<string, object> { 
                            { "CariListesi", cariResponse.Data },
                            { "DefaultNote", "" } 
                        },
                        new DialogOptions { Width = "500px", CloseDialogOnEsc = true });

                    if (dialogResult == null) // User Cancelled
                    {
                        _isAddingToCart = false;
                        return;
                    }

                    var selection = (OtoIsmailOrderDialog.OtoIsmailOrderDialogResult)dialogResult;
                    
                    // 3. Prepare Items
                    var cartItems = new List<ecommerce.Domain.Shared.Dtos.OtoIsmail.OtoIsmailCartItemDto>();
                    foreach (var item in orderitemsGrouped)
                    {
                        if (!string.IsNullOrEmpty(item.Item.SourceId) && int.TryParse(item.Item.SourceId, out int sourceId))
                        {
                            var productCode = await OrderService.GetProductCodeForCart(sellerId.Value, sourceId);
                            if (!string.IsNullOrEmpty(productCode))
                            {
                                // IMPORTANT: OtoIsmail API might expect quantity as int primarily, Dto has int. 
                                // Ensure matching logic.
                                cartItems.Add(new ecommerce.Domain.Shared.Dtos.OtoIsmail.OtoIsmailCartItemDto { StokKodu = productCode, Adet = item.Item.Quantity });
                            }
                        }
                    }

                    if (!cartItems.Any())
                    {
                        NotificationService.Notify(NotificationSeverity.Warning, "Siparişe eklenecek ürün bulunamadı (Eşleşen ürün kodu yok).");
                        _isAddingToCart = false;
                        return;
                    }

                    // 4. Send Order
                    // User requested BizAlacagiz to be always false, so we force it here regardless of dialog (or if dialog default is checked).
                    // Actually, if we want to respect user request "always no", we pass false.
                    var result = await OtoIsmailService.SendOrderAsync(selection.CariKodu, selection.OrderNote, false, cartItems);

                    if (result?.Success == true)
                    {
                         // Update local state
                        foreach (var item in order.OrderItems.Where(x => !string.IsNullOrEmpty(x.SourceId))) // Update items that likely participated
                        {
                            // Ideally match specifically what we sent if needed, but for now mark all eligible items
                            item.IsSellerOrderStatus = true;
                            item.SellerOrderResult = result.Message?.ToString(); // Message contains Order ID
                            try { await OrderService.UpdateOrderItem(item); } catch { }
                        }
                        NotificationService.Notify(NotificationSeverity.Success, "Sipariş Başarıyla Oluşturuldu", $"Sipariş ID: {result.Message}");
                    }
                    else
                    {
                        NotificationService.Notify(NotificationSeverity.Error, "Sipariş Oluşturulamadı", result?.Message?.ToString());
                    }

                    _isAddingToCart = false;
                    StateHasChanged();
                    return; // Exit OtoIsmail flow
                }

                bool basketSuccess = false;
                string basketMessage = "";
                
                // 1. ADD TO BASKET
                if (sellerId == 3) // Dega
                {
                     var cartItems = new List<Dega.Dtos.DegaCartItemDto>();
                     var itemsToUpdateBasketStatus = new List<OrderItems>();
                     
                     foreach (var item in orderitemsGrouped)
                     {
                         if (!string.IsNullOrEmpty(item.Item.SourceId) && int.TryParse(item.Item.SourceId, out int sourceId))
                         {
                             var productCode = await OrderService.GetProductCodeForCart(sellerId.Value, sourceId);
                             if (!string.IsNullOrEmpty(productCode))
                             {
                                 cartItems.Add(new Dega.Dtos.DegaCartItemDto { ProductCode = productCode, Quantity = item.Item.Quantity });
                                 itemsToUpdateBasketStatus.Add(item.Item);
                             }
                         }
                     }

                     if (cartItems.Any())
                     {
                         var result = await DegaService.AddToCartAsync(cartItems);
                         basketSuccess = result?.status == "succeeded";
                         basketMessage = result?.message?.Items?.FirstOrDefault()?.Msg ?? (basketSuccess ? "Sepet Oluşturuldu" : "Sepet Hatası");
                         
                         if (basketSuccess)
                         {
                             foreach (var itm in itemsToUpdateBasketStatus)
                             {
                                 try 
                                 {
                                    itm.IsSellerBasketStatus = true;
                                    await OrderService.UpdateOrderItem(itm); 
                                 }
                                 catch{} 
                             }
                         }
                     }
                     else
                     {
                         NotificationService.Notify(NotificationSeverity.Warning, "Sepete eklenecek uygun ürün bulunamadı.");
                         return; 
                     }
                }
                else if (sellerId == 4) // Remar
                {
                     var cartItems = new List<Remar.Dtos.RemarCartItemDto>();
                     var itemsToUpdateBasketStatus = new List<OrderItems>();
                     
                     foreach (var item in orderitemsGrouped)
                     {
                         if (!string.IsNullOrEmpty(item.Item.SourceId) && int.TryParse(item.Item.SourceId, out int sourceId))
                         {
                             var productCode = await OrderService.GetProductCodeForCart(sellerId.Value, sourceId);
                             if (!string.IsNullOrEmpty(productCode))
                             {
                                 cartItems.Add(new Remar.Dtos.RemarCartItemDto { ProductCode = productCode, Quantity = item.Item.Quantity });
                                 itemsToUpdateBasketStatus.Add(item.Item);
                             }
                         }
                     }

                     if (cartItems.Any())
                     {
                         var result = await RemarApiService.AddToCartAsync(cartItems);
                         basketSuccess = result?.status == "succeeded";
                         basketMessage = result?.message?.Items?.FirstOrDefault()?.Msg ?? (basketSuccess ? "Sepet Oluşturuldu" : "Sepet Hatası");
                         
                         if (basketSuccess)
                         {
                             foreach (var itm in itemsToUpdateBasketStatus)
                             {
                                 try 
                                 {
                                    itm.IsSellerBasketStatus = true;
                                    await OrderService.UpdateOrderItem(itm);
                                 }
                                 catch{}
                             }
                         }
                     }
                     else
                     {
                         NotificationService.Notify(NotificationSeverity.Warning, "Sepete eklenecek uygun ürün bulunamadı.");
                         return;
                     }
                }

                if (!basketSuccess)
                {
                    NotificationService.Notify(NotificationSeverity.Error, "Sepete Ekleme Başarısız", basketMessage);
                    return; 
                }

                // 2. CREATE ORDER
                bool orderSuccess = false;
                string orderMessage = "";
                string supplierOrderNo = "";

                string orderNote = $"Sipariş No: {order.Id} - {order.CustomerName}"; 
                string refNo = order.OrderNumber ?? order.Id.ToString();

                if (sellerId == 3) // Dega
                {
                    var req = new Dega.Dtos.CustomOrderRequestDto 
                    { 
                        OrderNote = orderNote,
                        ReferenceNo = refNo,
                        Items = new List<Dega.Dtos.CustomOrderItemDto>() 
                    };
                    
                    foreach (var item in orderitemsGrouped)
                    {
                         if (!string.IsNullOrEmpty(item.Item.SourceId) && int.TryParse(item.Item.SourceId, out int sourceId))
                         {
                             var productCode = await OrderService.GetProductCodeForCart(sellerId.Value, sourceId);
                             if (!string.IsNullOrEmpty(productCode))
                             {
                                 req.Items.Add(new Dega.Dtos.CustomOrderItemDto 
                                 { 
                                     ProductCode = productCode, 
                                     Quantity = (double)item.Item.Quantity,
                                     ItemExp = "" 
                                 });
                             }
                         }
                    }

                    var result = await DegaService.CreateOrderAsync(req);
                    orderSuccess = result?.Status == "succeeded";
                    orderMessage = result?.Message ?? (orderSuccess ? "Sipariş Başarılı" : "Sipariş Hatası");
                    supplierOrderNo = result?.OrderNo;
                }
                else if (sellerId == 4) // Remar
                {
                    var req = new Remar.Dtos.CustomOrderRequestDto 
                    { 
                        OrderNote = orderNote,
                        ReferenceNo = refNo,
                        Items = new List<Remar.Dtos.CustomOrderItemDto>() 
                    };
                    
                    foreach (var item in orderitemsGrouped)
                    {
                         if (!string.IsNullOrEmpty(item.Item.SourceId) && int.TryParse(item.Item.SourceId, out int sourceId))
                         {
                             var productCode = await OrderService.GetProductCodeForCart(sellerId.Value, sourceId);
                             if (!string.IsNullOrEmpty(productCode))
                             {
                                 req.Items.Add(new Remar.Dtos.CustomOrderItemDto 
                                 { 
                                     ProductCode = productCode, 
                                     Quantity = (double)item.Item.Quantity,
                                     ItemExp = "" 
                                 });
                             }
                         }
                    }

                    var result = await RemarApiService.CreateOrderAsync(req);
                    orderSuccess = result?.Status == "succeeded"; 
                    orderMessage = result?.Message ?? (orderSuccess ? "Sipariş Başarılı" : "Sipariş Hatası");
                    supplierOrderNo = result?.OrderNo;
                }

                if (orderSuccess)
                {
                    NotificationService.Notify(NotificationSeverity.Success, "Tedarikçi Siparişi Oluşturuldu", $"Sipariş No: {supplierOrderNo}");
                    
                    foreach (var item in orderitemsGrouped)
                    {
                        if (item.Item.IsSellerBasketStatus) 
                        {
                            try
                            {
                                item.Item.IsSellerOrderStatus = true;
                                item.Item.SellerOrderResult = $"{supplierOrderNo} - {orderMessage}";
                                await OrderService.UpdateOrderItem(item.Item);
                            }
                            catch{}
                        }
                    }
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, "Sipariş Oluşturulamadı", orderMessage);
                     foreach (var item in orderitemsGrouped)
                    {
                        if (item.Item.IsSellerBasketStatus)
                        {
                            try
                            {
                                item.Item.SellerOrderResult = $"FAIL: {orderMessage}";
                                await OrderService.UpdateOrderItem(item.Item);
                            }
                            catch{}
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "İşlem sırasında hata oluştu", ex.Message);
            }
            finally
            {
                _isAddingToCart = false;
                StateHasChanged();
            }
        }

        protected async Task CancelSupplierOrder()
        {
            if (order?.Seller?.Id != 1) return; // Only OtoIsmail for now

            if (await DialogService.Confirm("Tedarikçi siparişi İPTAL edilecek. Bu işlem geri alınamaz. Devam etmek istiyor musunuz?", "İptal Onayı", 
                new ConfirmOptions { OkButtonText = "Evet, İptal Et", CancelButtonText = "Hayır" }) != true)
            {
                return;
            }

            try
            {
                var firstItem = order.OrderItems.FirstOrDefault(x => x.IsSellerOrderStatus == true && !string.IsNullOrEmpty(x.SellerOrderResult));
                if (firstItem == null)
                {
                    NotificationService.Notify(NotificationSeverity.Warning, "İptal edilecek aktif bir tedarikçi siparişi bulunamadı.");
                    return;
                }

                // Extract Order ID from SellerOrderResult
                string orderId = firstItem.SellerOrderResult;
                
                // First check if order exists
                var statusCheck = await OtoIsmailService.GetOrderStatusAsync(orderId);
                if (statusCheck == null || statusCheck.Data == null || !statusCheck.Data.Any())
                {
                    NotificationService.Notify(NotificationSeverity.Warning, 
                        "Sipariş Bulunamadı", 
                        $"OtoIsmail'de sipariş bulunamadı (ID: {orderId}). Sipariş zaten iptal edilmiş veya farklı bir hesapta oluşturulmuş olabilir.");
                    return;
                }
                
                // Get order status
                var orderInfo = statusCheck.Data.FirstOrDefault();
                var currentStatus = orderInfo?.Durum ?? "Bilinmiyor";
                
                var result = await OtoIsmailService.CancelOrderAsync(orderId);

                if (result?.Success == true)
                {
                    NotificationService.Notify(NotificationSeverity.Success, "Tedarikçi siparişi başarıyla iptal edildi.");
                    
                    // Update local state
                    foreach (var item in order.OrderItems.Where(x => x.IsSellerOrderStatus == true))
                    {
                        item.IsSellerOrderStatus = false;
                        item.SellerOrderResult = $"İPTAL EDİLDİ ({DateTime.Now:dd.MM.yyyy HH:mm}) - {result.Message}";
                        try { await OrderService.UpdateOrderItem(item); } catch { }
                    }
                    StateHasChanged();
                }
                else
                {
                     NotificationService.Notify(NotificationSeverity.Error, "İptal Başarısız", result?.Message?.ToString());
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "İptal sırasında hata", ex.Message);
            }
        }

        /// <summary>
        /// Bağlı e-Fatura PDF'ini Odaksoft üzerinden indirir
        /// </summary>
        protected async Task DownloadLinkedEInvoicePdf(OrderLinkedInvoiceDto linkedInvoice)
        {
            if (_isDownloadingEInvoice || string.IsNullOrEmpty(linkedInvoice.Ettn)) return;

            try
            {
                _isDownloadingEInvoice = true;
                StateHasChanged();

                var response = await OdaksoftInvoiceService.DownloadOutboxInvoiceAsync(linkedInvoice.Ettn);

                if (response != null && response.Status)
                {
                    if (!string.IsNullOrEmpty(response.Html))
                    {
                        // HTML döndüyse yeni sekmede aç
                        await JSRuntime.InvokeVoidAsync("openHtmlInNewTab", new object?[] { response.Html });
                    }
                    else if (!string.IsNullOrEmpty(response.ByteArray))
                    {
                        // PDF olarak indir (ByteArray zaten base64 string)
                        var fileName = $"e-Fatura_{linkedInvoice.InvoiceNo}_{linkedInvoice.Ettn}.pdf";
                        await JSRuntime.InvokeVoidAsync("downloadFileFromBase64", new object?[] { response.ByteArray, fileName, "application/pdf" });
                    }
                    else
                    {
                        NotificationService.Notify(NotificationSeverity.Warning, "PDF İndirilemedi", "Odaksoft'tan fatura verisi alınamadı.");
                    }
                }
                else
                {
                    var errorMsg = response?.ExceptionMessage ?? response?.Message ?? "Bilinmeyen hata";
                    NotificationService.Notify(NotificationSeverity.Warning, "PDF İndirilemedi", errorMsg);
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "e-Fatura indirme hatası", ex.Message);
            }
            finally
            {
                _isDownloadingEInvoice = false;
                StateHasChanged();
            }
        }

        protected async Task OnTabChange(int index)
        {
            // Index 3 corresponds to the new "Tedarikci Depo" tab
            // "Sipariş Bilgileri" = 0
            // "Kargo Bilgileri" = 1
            // "Ürün Bilgileri" = 2
            // "Tedarikci Depo" = 3
            if (index == 3)
            {
                if (!_isStocksChecked && !_isCheckingStocks)
                {
                    // Update UI state before heavy work
                    await Task.Yield();
                    await CheckStocks();
                }
            }
        }
    }
}
