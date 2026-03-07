using ecommerce.Admin.Domain.Dtos.PriceListDto;
using ecommerce.Admin.Domain.Dtos.ProductDto;
using ecommerce.Admin.Domain.Dtos.HierarchicalDto;
using ecommerce.Admin.Domain.Dtos.WarehouseDto;
using ecommerce.Admin.Services;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Core.Helpers;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertPriceList
    {
        #region Injection
        [Inject] protected IJSRuntime JSRuntime { get; set; } = null!;
        [Inject] protected NavigationManager NavigationManager { get; set; } = null!;
        [Inject] protected DialogService DialogService { get; set; } = null!;
        [Inject] protected TooltipService TooltipService { get; set; } = null!;
        [Inject] protected ContextMenuService ContextMenuService { get; set; } = null!;
        [Inject] protected NotificationService NotificationService { get; set; } = null!;
        [Inject] protected AuthenticationService Security { get; set; } = null!;
        [Inject] public IPriceListService PriceListService { get; set; } = null!;
        [Inject] public IProductService ProductService { get; set; } = null!;
        [Inject] public ICustomerService CustomerService { get; set; } = null!;
        [Inject] public IWarehouseService WarehouseService { get; set; } = null!;
        [Inject] public ICurrencyAdminService CurrencyAdminService { get; set; } = null!;
        [Inject] public ICorporationService CorporationService { get; set; } = null!;
        [Inject] public IBranchService BranchService { get; set; } = null!;
        #endregion

        [Parameter] public int? Id { get; set; }

        protected PriceListUpsertDto? PriceList { get; set; }
        protected bool Saving { get; set; }
        
        protected IEnumerable<ecommerce.Admin.Services.Dtos.SelectItemDto<int?>> CustomerOptions { get; set; } = new List<ecommerce.Admin.Services.Dtos.SelectItemDto<int?>>();
        protected IEnumerable<CorporationListDto> CorporationOptions { get; set; } = new List<CorporationListDto>();
        protected IEnumerable<BranchListDto> BranchOptions { get; set; } = new List<BranchListDto>();
        protected IEnumerable<WarehouseListDto> WarehouseOptions { get; set; } = new List<WarehouseListDto>();
        protected IEnumerable<ecommerce.Admin.Services.Dtos.SelectItemDto<int?>> CurrencyOptions { get; set; } = new List<ecommerce.Admin.Services.Dtos.SelectItemDto<int?>>();
        protected List<ProductListDto> productSearchResults { get; set; } = new();
        protected string? selectedProductCode;
        protected ProductListDto? selectedProduct;
        protected RadzenDataGrid<PriceListItemUpsertDto>? itemsGrid;

        protected bool IsHeaderValid => PriceList != null &&
                                       !string.IsNullOrWhiteSpace(PriceList.Name) &&
                                       PriceList.CorporationId > 0 &&
                                       PriceList.BranchId > 0 &&
                                       PriceList.CurrencyId.HasValue;

        protected override async Task OnInitializedAsync()
        {
            await LoadData();

            if (Id.HasValue)
            {
                var response = await PriceListService.GetPriceListById(Id.Value);
                if (response.Ok && response.Result != null)
                {
                    var result = response.Result;
                    result.Items ??= new List<PriceListItemUpsertDto>();
                    // Önce seçili cariyi CustomerOptions'a ekle ki dropdown ilk render'da değeri göstersin
                    await EnsureCustomerInOptionsForCustomerId(result.CustomerId);
                    PriceList = result;
                    await PreFillHierarchicalData();
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                    PriceList = new PriceListUpsertDto
                    {
                        Code = GeneratePriceListCode(),
                        StartDate = DateTime.Now,
                        IsActive = true,
                        Items = new List<PriceListItemUpsertDto>()
                    };
                }
            }
            else
            {
                PriceList = new PriceListUpsertDto
                {
                    Code = GeneratePriceListCode(),
                    StartDate = DateTime.Now,
                    IsActive = true,
                    Items = new List<PriceListItemUpsertDto>()
                };
            }
        }

        private async Task LoadData()
        {
            var customerRs = await CustomerService.GetPagedCustomers(new Core.Models.PageSetting
            {
                Filter = string.Empty,
                OrderBy = "Id desc",
                Skip = 0,
                Take = 100
            });

            if (customerRs.Ok && customerRs.Result?.Data != null)
            {
                // RadzenDropDown benzersiz Value gerektirir — aynı Id’li cariler tekilleştirilir
                CustomerOptions = customerRs.Result.Data
                    .DistinctBy(c => c.Id)
                    .Select(c => new ecommerce.Admin.Services.Dtos.SelectItemDto<int?> { Text = $"{c.Code} - {c.Name}", Value = c.Id })
                    .ToList();
            }

            // Corporations (Hierarchical Root)
            var corpRs = await CorporationService.GetAllActiveCorporations();
            if (corpRs.Ok && corpRs.Result != null)
            {
                CorporationOptions = corpRs.Result;
            }

            // Currencies
            var curRs = await CurrencyAdminService.GetCurrencies();
            if (curRs.Ok && curRs.Result != null)
            {
                var currencies = curRs.Result
                    .OrderByDescending(c => c.CurrencyCode == "TRY" || c.CurrencyCode == "TL" || c.CurrencyCode == "TRL")
                    .ThenBy(c => c.CurrencyCode);

                // Fiyat listesinde artık CurrencyId tutuluyor. Dropdown'da Id değeri dönecek.
                CurrencyOptions = currencies
                    .Select(c => new ecommerce.Admin.Services.Dtos.SelectItemDto<int?>
                    {
                        Text = $"{c.CurrencyCode} - {c.CurrencyName}",
                        Value = c.Id
                    })
                    .ToList();
            }
        }

        // AutoComplete için ürünleri Products sayfasındaki servisle getir
        // AutoComplete için ürünleri Products sayfasındaki servisle getir
        protected async Task LoadProducts(LoadDataArgs args)
        {
            try
            {
                var search = args.Filter;
                if (string.IsNullOrWhiteSpace(search) || search.Length < 3)
                {
                    productSearchResults = new List<ProductListDto>();
                    return;
                }

                var rs = await ProductService.SearchProducts(search);
                if (rs.Ok && rs.Result != null)
                {
                    productSearchResults = rs.Result;
                }
                else
                {
                    productSearchResults = new List<ProductListDto>();
                }
                
                await InvokeAsync(StateHasChanged);
            }
            catch
            {
                productSearchResults = new List<ProductListDto>();
            }
        }

        // AutoComplete'te ürün seçildiğinde
        protected void OnProductSearchChanged(object? value)
        {
            // AutoComplete'te seçim yapıldığında
        }

        // Üstteki aramalı dropdown'dan ürün seçip satır ekle
        protected async Task AddProductFromSearch()
        {
            try
            {
                ProductListDto? product = null;

                if (!string.IsNullOrWhiteSpace(selectedProductCode))
                {
                    // selectedProductCode artık Id (IdStr) veya Barkod olabilir.
                    
                    // 1. Önce listeden IdStr ile bulmaya çalış (Dropdown seçimi)
                    product = productSearchResults.FirstOrDefault(p => p.IdStr == selectedProductCode);
                    
                    // 2. Bulamazsa Barkod veya İsim ile listede ara (Manuel giriş)
                    if (product == null)
                    {
                         product = productSearchResults.FirstOrDefault(p => 
                            p.Barcode == selectedProductCode || 
                            p.Name.Contains(selectedProductCode, StringComparison.OrdinalIgnoreCase));
                    }

                    // 3. Hala yoksa servisten ara (Listede yoksa ama DB'de varsa)
                    if (product == null)
                    {
                         var rs = await ProductService.SearchProducts(selectedProductCode);
                         product = rs.Ok && rs.Result != null ? rs.Result.FirstOrDefault() : null;
                    }
                }

                if (product == null)
                {
                    NotificationService.Notify(NotificationSeverity.Warning, "Ürün bulunamadı. Lütfen listeden seçin.");
                    return;
                }

                // Aynı ürün zaten ekli mi kontrol et
                if (PriceList?.Items != null && PriceList.Items.Any(i => i.ProductId == product.Id))
                {
                    NotificationService.Notify(NotificationSeverity.Warning, "Bu ürün zaten listede mevcut.");
                    return;
                }

                if (PriceList == null)
                {
                    PriceList = new PriceListUpsertDto
                    {
                        Items = new List<PriceListItemUpsertDto>()
                    };
                }

                PriceList.Items ??= new List<PriceListItemUpsertDto>();

                PriceList.Items.Insert(0, new PriceListItemUpsertDto
                {
                    Order = 1,
                    ProductId = product.Id,
                    ProductName = product.Name ?? string.Empty,
                    CostPrice = product.CostPrice,
                    SalePrice = product.Price > 0 ? product.Price : (product.RetailPrice ?? 0)
                });
                
                // Sıralamayı güncelle
                for (int i = 0; i < PriceList.Items.Count; i++)
                {
                    PriceList.Items[i].Order = i + 1;
                }

                selectedProductCode = string.Empty;
                selectedProduct = null;
                productSearchResults.Clear();
                
                if (itemsGrid != null)
                {
                    await itemsGrid.Reload();
                }
                else
                {
                    StateHasChanged();
                }

                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Başarılı",
                    Detail = $"{product.Name} ürünü başarıyla eklendi."
                });
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, $"Ürün eklenirken hata: {ex.Message}");
            }
        }

        protected async Task OnProductSelected(string value, PriceListItemUpsertDto item)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            var pager = new Core.Models.PageSetting
            {
                Filter = $"Name.Contains(\"{value}\") or Barcode.Contains(\"{value}\")",
                OrderBy = "Id desc",
                Skip = 0,
                Take = 20
            };

            var rs = await ProductService.GetProducts(pager);
            if (rs.Ok && rs.Result?.Data != null && rs.Result.Data.Any())
            {
                var first = rs.Result.Data.First();
                item.ProductId = first.Id;
                item.ProductName = first.Name;

                // İstersen varsayılan fiyatları da dolduralım
                if (item.SalePrice == 0)
                    item.SalePrice = first.Price;
                if (item.CostPrice == 0)
                    item.CostPrice = first.CostPrice;
            }
        }

        private string GeneratePriceListCode()
        {
            return $"PL-{DateTime.Now:yyyy}-{new Random().Next(100, 999):000}";
        }

        protected void AddPriceListItem()
        {
            if (PriceList == null)
            {
                PriceList = new PriceListUpsertDto
                {
                    Items = new List<PriceListItemUpsertDto>()
                };
            }

            PriceList.Items ??= new List<PriceListItemUpsertDto>();

            PriceList.Items.Add(new PriceListItemUpsertDto
            {
                Order = PriceList.Items.Count + 1,
                CostPrice = 0,
                SalePrice = 0
            });

            StateHasChanged();
        }

        protected async Task RemovePriceListItem(PriceListItemUpsertDto item)
        {
            try
            {
                if (PriceList == null || PriceList.Items == null)
                    return;

                var productName = !string.IsNullOrWhiteSpace(item.ProductName) 
                    ? item.ProductName 
                    : "Bu ürün";

                var confirm = await DialogService.Confirm(
                    $"{productName} ürününü listeden silmek istediğinizden emin misiniz?",
                    "Ürün Sil",
                    new ConfirmOptions { OkButtonText = "Evet", CancelButtonText = "Hayır" });

                if (confirm == true)
                {
                    PriceList.Items.Remove(item);
                    
                    // Reorder items
                    for (int i = 0; i < PriceList.Items.Count; i++)
                    {
                        PriceList.Items[i].Order = i + 1;
                    }

                    // Grid'i anlık güncelle
                    if (itemsGrid != null)
                    {
                        await itemsGrid.Reload();
                    }
                    else
                    {
                        StateHasChanged();
                    }

                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Başarılı",
                        Detail = "Ürün listeden silindi."
                    });
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, $"Silme işlemi sırasında hata: {ex.Message}");
            }
        }

        protected async Task FormSubmit(PriceListUpsertDto args)
        {
            try
            {
                Saving = true;

                // Validasyon: zorunlu alanlar (tenant yapısı + genel)
                if (string.IsNullOrWhiteSpace(args?.Code))
                {
                    NotificationService.Notify(NotificationSeverity.Warning, "Sirkü No zorunludur.");
                    return;
                }
                if (string.IsNullOrWhiteSpace(args?.Name))
                {
                    NotificationService.Notify(NotificationSeverity.Warning, "Sirkü Adı zorunludur.");
                    return;
                }
                if (!args!.CorporationId.HasValue || args.CorporationId.Value <= 0)
                {
                    NotificationService.Notify(NotificationSeverity.Warning, "Şirket seçimi zorunludur.");
                    return;
                }
                if (!args.BranchId.HasValue || args.BranchId.Value <= 0)
                {
                    NotificationService.Notify(NotificationSeverity.Warning, "Şube seçimi zorunludur.");
                    return;
                }
                if (!args.CurrencyId.HasValue || args.CurrencyId.Value <= 0)
                {
                    NotificationService.Notify(NotificationSeverity.Warning, "Döviz tipi zorunludur.");
                    return;
                }

                var response = await PriceListService.UpsertPriceList(new AuditWrapDto<PriceListUpsertDto>
                {
                    UserId = Security.User.Id,
                    Dto = args
                });

                if (response.Ok)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Başarılı",
                        Detail = "Fiyat listesi kaydedildi."
                    });
                    DialogService.Close(args);
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, ex.Message);
            }
            finally
            {
                Saving = false;
            }
        }
        private async Task LoadBranches(int corporationId)
        {
            var result = await BranchService.GetBranchesByCorporationId(corporationId);
            if (result.Ok && result.Result != null)
            {
                BranchOptions = result.Result;
            }
            else
            {
                BranchOptions = new List<BranchListDto>();
            }
            StateHasChanged();
        }

        private async Task LoadWarehouses(int branchId)
        {
            var result = await WarehouseService.GetWarehousesByBranchId(branchId);
            if (result.Ok && result.Result != null)
            {
                WarehouseOptions = result.Result;
            }
            else
            {
                WarehouseOptions = new List<WarehouseListDto>();
            }
            StateHasChanged();
        }

        protected async Task OnCorporationChange(object value)
        {
            PriceList.BranchId = null;
            PriceList.WarehouseId = null;
            BranchOptions = new List<BranchListDto>();
            WarehouseOptions = new List<WarehouseListDto>();

            if (value is int corpId && corpId > 0)
            {
                await LoadBranches(corpId);
            }
        }

        protected async Task OnBranchChange(object value)
        {
            PriceList.WarehouseId = null;
            WarehouseOptions = new List<WarehouseListDto>();

            if (value is int branchId && branchId > 0)
            {
                await LoadWarehouses(branchId);
            }
        }

        // Düzenlemede şirket/şube/depo dropdown'larını doldur: entity'den gelen Corp/Branch ile listeleri yükle
        // veya sadece WarehouseId varsa (eski veri) depodan şirket/şube çıkar.
        private async Task PreFillHierarchicalData()
        {
            if (PriceList == null) return;

            var corpId = PriceList.CorporationId.GetValueOrDefault(0);
            var branchId = PriceList.BranchId.GetValueOrDefault(0);

            // Senaryo 1: Şirket ve şube gelmişse listeleri yükle (edit'te seçili görünsün)
            if (corpId > 0)
            {
                await LoadBranches(corpId);
                if (branchId > 0)
                    await LoadWarehouses(branchId);
                return;
            }

            // Senaryo 2: Eski veri veya sadece WarehouseId var – depodan şube, şubeden şirket al
            if (PriceList.WarehouseId.GetValueOrDefault(0) > 0)
            {
                var whRs = await WarehouseService.GetWarehouseById(PriceList.WarehouseId!.Value);
                if (whRs.Ok && whRs.Result != null)
                {
                    var wh = whRs.Result;
                    PriceList.BranchId = wh.BranchId;
                    // Warehouse entity'de CorporationId yok; şirketi şubeden al
                    if (wh.CorporationId.GetValueOrDefault(0) <= 0 && wh.BranchId.GetValueOrDefault(0) > 0)
                    {
                        var branchRs = await BranchService.GetBranchById(wh.BranchId!.Value);
                        if (branchRs.Ok && branchRs.Result != null)
                            PriceList.CorporationId = branchRs.Result.CorporationId;
                    }
                    else
                        PriceList.CorporationId = wh.CorporationId;
                    var cId = PriceList.CorporationId.GetValueOrDefault(0);
                    var bId = PriceList.BranchId.GetValueOrDefault(0);
                    if (cId > 0) await LoadBranches(cId);
                    if (bId > 0) await LoadWarehouses(bId);
                }
            }

            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Verilen cari ID'si CustomerOptions'ta yoksa GetCustomerById ile ekler (veya yedek "Cari #Id").
        /// Düzenlemede PriceList atanmadan önce çağrılır ki dropdown ilk render'da seçili değeri göstersin.
        /// </summary>
        private async Task EnsureCustomerInOptionsForCustomerId(int? customerId)
        {
            if (customerId == null || customerId.Value <= 0)
                return;
            var id = customerId.Value;
            if (CustomerOptions?.Any(c => c.Value == id) == true)
                return;
            var rs = await CustomerService.GetCustomerById(id);
            var list = CustomerOptions?.ToList() ?? new List<ecommerce.Admin.Services.Dtos.SelectItemDto<int?>>();
            if (rs.Ok && rs.Result != null)
            {
                var c = rs.Result;
                list.Insert(0, new ecommerce.Admin.Services.Dtos.SelectItemDto<int?> { Text = $"{c.Code} - {c.Name}", Value = c.Id });
            }
            else
            {
                list.Insert(0, new ecommerce.Admin.Services.Dtos.SelectItemDto<int?> { Text = $"Cari #{id}", Value = id });
            }
            CustomerOptions = list;
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Düzenlemede seçili cari CustomerOptions'ta yoksa (ilk 100 caride olmayabilir) GetCustomerById ile ekler.
        /// GetCustomerById yetki nedeniyle başarısız olursa (örn. farklı şube) yedek seçenek "Cari #Id" eklenir ki dropdown değeri göstersin.
        /// </summary>
        private async Task EnsureCustomerInOptions()
        {
            await EnsureCustomerInOptionsForCustomerId(PriceList?.CustomerId);
        }
    }
}
