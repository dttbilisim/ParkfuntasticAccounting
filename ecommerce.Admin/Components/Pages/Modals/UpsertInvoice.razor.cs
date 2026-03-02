using ecommerce.Admin.Domain.Dtos.InvoiceDto;
using ecommerce.Admin.Domain.Dtos.Customer;
using ecommerce.Domain.Shared.Dtos.Filters;
using ecommerce.Domain.Shared.Dtos.Product;
using ecommerce.Admin.Domain.Dtos.InvoiceTypeDto;
using ecommerce.Admin.Domain.Dtos.WarehouseDto;
using ecommerce.Admin.Domain.Dtos.ProductDto;
using ecommerce.Admin.Domain.Dtos.SalesPersonDto;
using ecommerce.Admin.Domain.Dtos.CashRegisterDto;
using ecommerce.Admin.Services;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Admin.Domain.Dtos.ProductUnitDto;
using ecommerce.Admin.Domain.Dtos.CurrencyDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Domain.Dtos.PaymentTypeDto;
using ecommerce.Admin.Domain.Dtos.PcPosDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.Core.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using Microsoft.Extensions.Logging;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertInvoice
    {
        #region Injection
        [Inject] protected IJSRuntime JSRuntime { get; set; } = null!;
        [Inject] protected NavigationManager NavigationManager { get; set; } = null!;
        [Inject] protected DialogService DialogService { get; set; } = null!;
        [Inject] protected TooltipService TooltipService { get; set; } = null!;
        [Inject] protected ContextMenuService ContextMenuService { get; set; } = null!;
        [Inject] protected NotificationService NotificationService { get; set; } = null!;
        [Inject] protected AuthenticationService Security { get; set; } = null!;
        [Inject] public ICustomerService CustomerService { get; set; } = null!;
        [Inject] public IInvoiceTypeService InvoiceTypeService { get; set; } = null!;
        [Inject] public IWarehouseService WarehouseService { get; set; } = null!;
        [Inject] public IProductService ProductService { get; set; } = null!;
        [Inject] public ISalesPersonService SalesPersonService { get; set; } = null!;
        [Inject] public ICurrencyAdminService CurrencyAdminService { get; set; } = null!;
        [Inject] public IInvoiceService InvoiceService { get; set; } = null!;
        [Inject] public IProductUnitService ProductUnitService { get; set; } = null!;
        [Inject] public ICashRegisterService CashRegisterService { get; set; } = default!;
        [Inject] public IPaymentTypeService PaymentTypeService { get; set; } = null!;
        [Inject] public IPcPosService PcPosService { get; set; } = null!;
        [Inject] public IOrderService OrderService { get; set; } = null!;
        [Inject] public IAdminProductSearchService ProductSearchService { get; set; } = null!;
        [Inject] protected ITenantProvider TenantProvider { get; set; } = null!;
        [Inject] protected ILogger<UpsertInvoice> _logger { get; set; } = null!;
        #endregion

        [Parameter] public int? Id { get; set; }
        [Parameter] public int? OrderId { get; set; }
        [Parameter] public List<int>? OrderIds { get; set; }

        protected InvoiceUpsertDto? Invoice { get; set; }
        protected bool Saving { get; set; }
        
        protected List<CustomerListDto> Customers { get; set; } = new();
        protected IEnumerable<InvoiceTypeListDto> InvoiceTypes { get; set; } = new List<InvoiceTypeListDto>();
        protected IEnumerable<WarehouseListDto> WarehouseOptions { get; set; } = new List<WarehouseListDto>();
        public class InvoiceDropDownItem
        {
            public string Text { get; set; } = string.Empty;
            public object Value { get; set; }
            public int? PaymentTypeId { get; set; }
        }

        protected IEnumerable<InvoiceDropDownItem> DocumentTypeOptions { get; set; } = new List<InvoiceDropDownItem>();
        protected IEnumerable<InvoiceDropDownItem> SalesPersonOptions { get; set; } = new List<InvoiceDropDownItem>();
        protected IEnumerable<InvoiceDropDownItem> CurrencyOptions { get; set; } = new List<InvoiceDropDownItem>();
        protected IEnumerable<InvoiceDropDownItem> PaymentTypeOptions { get; set; } = new List<InvoiceDropDownItem>();
        protected IEnumerable<InvoiceDropDownItem> CashRegisterOptions { get; set; } = new List<InvoiceDropDownItem>();
        protected List<CashRegisterListDto> AllCashRegisters { get; set; } = new();
        protected IEnumerable<InvoiceDropDownItem> PcPosOptions { get; set; } = new List<InvoiceDropDownItem>();
        protected List<PaymentTypeListDto> PaymentTypes { get; set; } = new();
        protected List<CurrencyListDto> Currencies { get; set; } = new();
        protected List<ProductListDto> productSearchResults { get; set; } = new();
        protected string? selectedProductCode;
        protected string? selectedCustomerSearch;
        protected RadzenDataGrid<InvoiceItemUpsertDto>? itemsGrid;
        protected Dictionary<int, List<ProductUnitListDto>> ProductUnitsCache { get; set; } = new();
        protected bool isProductLoading = false;
        protected bool isCustomerLoading = false;
        private bool _isProcessingOrders = false;
        protected string? CustomerWorkingTypeStr { get; set; }
        protected int? CustomerMaturity { get; set; }
        
        // Sipariş detayları accordion için
        protected List<OrderAccordionItem> _orderAccordionItems { get; set; } = new();
        protected HashSet<int> _expandedOrderIds { get; set; } = new();

        /// <summary>
        /// KDV tutarlarının oran bazında dağılımı (RecalculateTotals ile doldurulur).
        /// </summary>
        protected List<(decimal Rate, decimal Amount)> VatAmountsByRate { get; set; } = new();
        
        /// <summary>
        /// Accordion'da gösterilecek sipariş özet bilgisi
        /// </summary>
        public class OrderAccordionItem
        {
            public int OrderId { get; set; }
            public string OrderNumber { get; set; } = "";
            public string CustomerName { get; set; } = "";
            public DateTime CreatedDate { get; set; }
            public decimal OrderTotal { get; set; }
            public decimal CargoPrice { get; set; }
            public string StatusText { get; set; } = "";
            public List<OrderAccordionLineItem> Items { get; set; } = new();
        }
        
        /// <summary>
        /// Accordion içindeki ürün satırı
        /// </summary>
        public class OrderAccordionLineItem
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; } = "";
            public int Quantity { get; set; }
            public decimal Price { get; set; }
            public decimal TotalPrice { get; set; }
            public decimal DiscountAmount { get; set; }
        }
        
        protected void ToggleOrderAccordion(int orderId)
        {
            if (_expandedOrderIds.Contains(orderId))
                _expandedOrderIds.Remove(orderId);
            else
                _expandedOrderIds.Add(orderId);
        }

        protected bool IsHeaderValid => Invoice != null &&
                                       Invoice.CustomerId.HasValue &&
                                       Invoice.InvoiceTypeId.HasValue &&
                                       Invoice.PaymentTypeId.HasValue &&
                                       // Kasa veya POS kontrolü: Eğer seçenekler varsa, en az birinin seçilmesi gerekir
                                       ((CashRegisterOptions.Any() && Invoice.CashRegisterId.HasValue) || 
                                        (PcPosOptions.Any() && Invoice.PcPosDefinitionId.HasValue) || 
                                        (!CashRegisterOptions.Any() && !PcPosOptions.Any())) &&
                                       !string.IsNullOrWhiteSpace(Invoice.InvoiceNo) &&
                                       !string.IsNullOrWhiteSpace(Invoice.InvoiceSerialNo) &&
                                       Invoice.WarehouseId.HasValue &&
                                       Invoice.CurrencyId.HasValue;

        protected RadzenTemplateForm<InvoiceUpsertDto>? invoiceForm;

        /// <summary>
        /// Fatura Odaksoft'a gönderilmiş veya işlem görmüşse readonly olur
        /// </summary>
        protected bool IsReadOnly => Invoice != null && !string.IsNullOrEmpty(Invoice.Ettn);
        protected async Task ManualSubmit()
        {
            if (invoiceForm != null && invoiceForm.EditContext.Validate())
            {
                await FormSubmit(Invoice!);
            }
        }
        
        protected bool IsTry => Invoice?.CurrencyId.HasValue == true && Currencies.FirstOrDefault(c => c.Id == Invoice.CurrencyId)?.CurrencyCode == "TRY";
        
        // Tab yönetimi için

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
            
            if (Id.HasValue)
            {
                var response = await InvoiceService.GetInvoiceById(Id.Value);
                if (response.Ok && response.Result != null)
                {
                    Invoice = response.Result;
                    if (string.IsNullOrEmpty(Invoice.InvoiceSerialNo))
                    {
                        Invoice.InvoiceSerialNo = "A";
                    }
                    
                    // Cari dropdown'unu set et (RadzenAutoComplete string binding kullanıyor)
                    if (!string.IsNullOrEmpty(Invoice.CustomerName))
                    {
                        selectedCustomerSearch = Invoice.CustomerName;
                    }
                    
                    // Depo seçimini doğrula — WarehouseOptions'da var mı kontrol et
                    if (Invoice.WarehouseId.HasValue && Invoice.WarehouseId > 0)
                    {
                        var whMatch = WarehouseOptions?.FirstOrDefault(w => w.Id == Invoice.WarehouseId.Value);
                        if (whMatch == null)
                        {
                            _logger.LogWarning("[OnInit] WarehouseId {WarehouseId} WarehouseOptions listesinde bulunamadı. Fallback ekleniyor. Liste sayısı: {Count}", 
                                Invoice.WarehouseId, WarehouseOptions?.Count() ?? 0);
                            
                            // Depo branch filtresi yüzünden listede yok — fallback olarak ekle
                            var fallbackWh = new ecommerce.Admin.Domain.Dtos.WarehouseDto.WarehouseListDto
                            {
                                Id = Invoice.WarehouseId.Value,
                                Name = !string.IsNullOrEmpty(Invoice.Warehouse) ? Invoice.Warehouse : $"Depo #{Invoice.WarehouseId.Value}"
                            };
                            WarehouseOptions = (WarehouseOptions ?? new List<ecommerce.Admin.Domain.Dtos.WarehouseDto.WarehouseListDto>())
                                .Append(fallbackWh)
                                .ToList();
                        }
                        else
                        {
                            // Depo adını da set et
                            Invoice.Warehouse = whMatch.Name;
                        }
                    }
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                    Invoice = new InvoiceUpsertDto
                    {
                        Items = new List<InvoiceItemUpsertDto>()
                    };
                }
            }
            else if (OrderIds != null && OrderIds.Any())
            {
                await LoadInvoiceFromOrders(OrderIds);
            }
            else if (OrderId.HasValue)
            {
                // Tek sipariş geldiyse listeye çevirip gönder
                await LoadInvoiceFromOrders(new List<int> { OrderId.Value });
            }
            else
            {
                Invoice = new InvoiceUpsertDto
                {
                    InvoiceDate = DateTime.Now,
                    InvoiceNo = GenerateInvoiceNo(),
                    IsVatIncluded = true,
                    IsActive = true,
                    ExchangeRate = 1,
                    BranchId = TenantProvider.GetCurrentBranchId(),
                    Items = new List<InvoiceItemUpsertDto>()
                };
                Invoice.InvoiceSerialNo = "A";

                var tryCurrency = Currencies.FirstOrDefault(c => c.CurrencyCode == "TRY");
                if (tryCurrency != null)
                {
                    Invoice.CurrencyId = tryCurrency.Id;
                    Invoice.CurrencyCode = "TRY";
                }
            }

            if (Invoice?.CustomerId.HasValue == true)
            {
                await SetCustomerInfo(Invoice.CustomerId.Value);
            }

            if (Invoice?.Items != null && Invoice.Items.Any())
            {
                var productIds = Invoice.Items
                    .Where(i => i.ProductId.HasValue)
                    .Select(i => i.ProductId!.Value)
                    .Distinct()
                    .ToList();

                if (productIds.Any())
                {
                    var unitsRs = await ProductUnitService.GetProductUnitsByProductIds(productIds);
                    if (unitsRs.Ok && unitsRs.Result != null)
                    {
                        var allUnits = unitsRs.Result;
                        foreach (var pid in productIds)
                        {
                            ProductUnitsCache[pid] = allUnits.Where(u => u.ProductId == pid).ToList();
                        }
                        // Birim dropdown'larının doğru dolu görünmesi için cache dolduktan sonra yeniden çizim
                        StateHasChanged();
                    }
                }
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && (OrderId.HasValue || (OrderIds != null && OrderIds.Any())) && Invoice?.Items != null && Invoice.Items.Any() && itemsGrid != null)
            {
                Console.WriteLine($"[OnAfterRenderAsync] First render with OrderId(s), reloading itemsGrid. Items count: {Invoice.Items.Count}");
                await itemsGrid.Reload();
                await InvokeAsync(StateHasChanged);
            }
            await base.OnAfterRenderAsync(firstRender);
        }

        private async Task LoadData()
        {
            // Sıralı çağrı — aynı scoped DbContext paylaşıldığı için paralel sorgu yapılamaz (NpgsqlOperationInProgressException)
            
            // Cariler
            var customerRs = await CustomerService.GetPagedCustomersForInvoice(new PageSetting
            {
                Filter = string.Empty,
                OrderBy = "Id desc",
                Skip = 0,
                Take = 200
            });
            if (customerRs.Ok && customerRs.Result?.Data != null)
            {
                Customers = customerRs.Result.Data;
            }

            // Fatura tipleri
            var invTypeRs = await InvoiceTypeService.GetInvoiceTypesForInvoice();
            if (invTypeRs.Ok && invTypeRs.Result != null)
            {
                InvoiceTypes = invTypeRs.Result;
            }

            // Depolar
            var whRs = await WarehouseService.GetAllWarehouses();
            if (whRs.Ok && whRs.Result != null)
            {
                WarehouseOptions = whRs.Result;
            }

            // Belge tipleri
            DocumentTypeOptions = new List<InvoiceDropDownItem>
            {
                new InvoiceDropDownItem { Text = "Satış", Value = "Satış" },
                new InvoiceDropDownItem { Text = "İade", Value = "İade" }
            };

            // Plasiyerler
            var salesPersonRs = await SalesPersonService.GetSalesPersons();
            if (salesPersonRs.Ok && salesPersonRs.Result != null)
            {
                SalesPersonOptions = salesPersonRs.Result.Select(sp => new InvoiceDropDownItem
                {
                    Text = $"{sp.FirstName} {sp.LastName}",
                    Value = (int?)sp.Id
                }).ToList();
            }

            // Dövizler
            var currencyRs = await CurrencyAdminService.GetCurrencies();
            if (currencyRs.Ok && currencyRs.Result != null)
            {
                Currencies = currencyRs.Result
                    .GroupBy(c => c.CurrencyCode)
                    .Select(g => g.OrderByDescending(c => c.CreatedDate).First())
                    .ToList();

                CurrencyOptions = Currencies
                    .Select(c => new InvoiceDropDownItem
                    {
                        Text = $"{c.CurrencyCode} - {c.CurrencyName} ({(c.CurrencyCode == "TRY" ? "1,0000" : c.ForexSelling.ToString("N4"))})",
                        Value = (int?)c.Id
                    })
                    .OrderBy(x => {
                        var code = Currencies.FirstOrDefault(c => c.Id == (int?)x.Value)?.CurrencyCode;
                        return code switch {
                            "TRY" => 0,
                            "EUR" => 1,
                            "USD" => 2,
                            _ => 3
                        };
                    })
                    .ThenBy(x => x.Text)
                    .ToList();
            }

            // Ödeme tipleri (Tablodan)
            var paymentTypeRs = await PaymentTypeService.GetAllPaymentTypesForInvoice();
            if (paymentTypeRs.Ok && paymentTypeRs.Result != null)
            {
                PaymentTypes = paymentTypeRs.Result;
                PaymentTypeOptions = PaymentTypes.Select(pt => new InvoiceDropDownItem
                {
                    Text = pt.Name,
                    Value = pt.Id
                }).ToList();
            }
            else
            {
                PaymentTypeOptions = new List<InvoiceDropDownItem>();
            }

            // POS Tanımları
            var pcPosRs = await PcPosService.GetPcPos();
            if (pcPosRs.Ok && pcPosRs.Result != null)
            {
                PcPosOptions = pcPosRs.Result.Select(p => new InvoiceDropDownItem
                {
                    Text = p.Name,
                    Value = p.Id,
                    PaymentTypeId = p.PaymentTypeId
                }).ToList();
            }

            // Kasalar
            var cashRegisterRs = await CashRegisterService.GetCashRegisters();
            if (cashRegisterRs.Ok && cashRegisterRs.Result != null)
            {
                // Tüm kasaları sakla
                AllCashRegisters = cashRegisterRs.Result.ToList();
                
                // Tüm seçenekleri sakla, filtrelemeyi OnPaymentTypeChange içinde yapacağız
                // Ama ilk yüklemede de filtreli gelmeli eğer PaymentType seçiliyse
                CashRegisterOptions = cashRegisterRs.Result.Select(cr => new InvoiceDropDownItem
                {
                    Text = cr.Name,
                    Value = (int?)cr.Id,
                    PaymentTypeId = cr.PaymentTypeId
                }).ToList();
            }
            else
            {
                CashRegisterOptions = new List<InvoiceDropDownItem>();
                AllCashRegisters = new List<CashRegisterListDto>();
            }

            // Başlangıçta ödeme tipine göre filtrele
            if (Invoice?.PaymentTypeId.HasValue == true)
            {
                await OnPaymentTypeChange(Invoice.PaymentTypeId.Value);
            }
        }

        private string GenerateInvoiceNo()
        {
            return $"INV{DateTime.Now:yyyyMMdd}{new Random().Next(1000, 9999)}";
        }

        protected void OnPaymentTypeChangeSync(object value) { _ = InvokeAsync(async () => await OnPaymentTypeChange(value)); }
        protected async Task OnPaymentTypeChange(object value)
        {
            if (value is int paymentTypeId && Invoice != null)
            {
                var selectedType = PaymentTypes.FirstOrDefault(pt => pt.Id == paymentTypeId);
                if (selectedType != null)
                {
                    // 1. Kasaları Filtrele (Cach'den kullan)
                    var linkedRegisters = AllCashRegisters.Where(cr => cr.PaymentTypeId == paymentTypeId).ToList();

                    if (linkedRegisters.Any())
                    {
                        CashRegisterOptions = linkedRegisters.Select(cr => new InvoiceDropDownItem 
                        { 
                            Text = cr.Name, 
                            Value = (int?)cr.Id 
                        }).ToList();
                        
                        // İlk kasayı otomatik seçebiliriz veya boş bırakabiliriz
                        if (Invoice.CashRegisterId == null || !linkedRegisters.Any(r => r.Id == Invoice.CashRegisterId))
                        {
                            Invoice.CashRegisterId = linkedRegisters.First().Id;
                        }
                    }
                    else
                    {
                        CashRegisterOptions = new List<InvoiceDropDownItem>();
                        Invoice.CashRegisterId = null;
                    }
                }
            }
            else if (Invoice != null)
            {
                Invoice.CashRegisterId = null;
            }
            await Task.CompletedTask;
        }

        protected void OnCurrencyChangeSync(object value) { _ = InvokeAsync(async () => await OnCurrencyChange(value)); }
        protected async Task OnCurrencyChange(object value)
        {
            if (value is int currencyId && Invoice != null)
            {
                var currency = Currencies.FirstOrDefault(c => c.Id == currencyId);
                if (currency != null)
                {
                    Invoice.CurrencyCode = currency.CurrencyCode;
                    if (currency.CurrencyCode == "TRY")
                    {
                        Invoice.ExchangeRate = 1;
                    }
                    else
                    {
                        Invoice.ExchangeRate = currency.ForexSelling;
                    }
                }
                RecalculateTotals();
            }
        }

        protected void OnExchangeRateChange(decimal value)
        {
            if (Invoice != null)
            {
                Invoice.ExchangeRate = value;
                RecalculateTotals();
            }
        }

        protected void OnCustomerChangeSync(object? value) { _ = InvokeAsync(async () => await OnCustomerChange(value)); }
        protected async Task OnCustomerChange(object? value)
        {
            if (Invoice == null) return;

            CustomerListDto? customer = null;
            if (value is int customerId)
            {
                customer = Customers.FirstOrDefault(c => c.Id == customerId);
            }
            else if (value is string customerName && !string.IsNullOrWhiteSpace(customerName))
            {
                var trimmed = customerName.Trim();
                // Önce tam eşleşme, sonra içerik eşleşmesi
                customer = Customers.FirstOrDefault(c => c.Name == trimmed || c.Code == trimmed)
                        ?? Customers.FirstOrDefault(c => c.Name != null && c.Name.Trim() == trimmed);
            }

            if (customer != null)
            {
                Invoice.CustomerId = customer.Id;
                Invoice.CustomerName = customer.Name;
                selectedCustomerSearch = customer.Name;
                Invoice.RiskLimit = customer.RiskLimit;

                await SetCustomerInfo(customer.Id);
                
                // Müşteriye bağlı plasiyer'i otomatik set et
                var salesPersonsRs = await CustomerService.GetCustomerSalesPersons(customer.Id);
                if (salesPersonsRs.Ok && salesPersonsRs.Result != null && salesPersonsRs.Result.Any())
                {
                    // İlk plasiyer'i otomatik seç (birden fazla varsa ilkini al)
                    var firstSalesPerson = salesPersonsRs.Result.First();
                    Invoice.SalesPersonId = firstSalesPerson.SalesPersonId;
                    
                    // Eğer birden fazla plasiyer varsa kullanıcıya bilgi ver
                    if (salesPersonsRs.Result.Count > 1)
                    {
                        NotificationService.Notify(new NotificationMessage
                        {
                            Severity = NotificationSeverity.Info,
                            Summary = "Plasiyer Seçildi",
                            Detail = $"Bu müşteriye {salesPersonsRs.Result.Count} plasiyer bağlı. İlk plasiyer otomatik seçildi. Gerekirse değiştirebilirsiniz.",
                            Duration = 5000
                        });
                    }
                }
                else
                {
                    // Plasiyer bulunamadıysa temizle
                    Invoice.SalesPersonId = null;
                }
            }
            else if (value == null && Invoice != null)
            {
                Invoice.CustomerName = null;
                selectedCustomerSearch = null;
                Invoice.RiskLimit = 0;
                Invoice.AverageMaturity = 0;
                Invoice.SalesPersonId = null; // Müşteri temizlendiğinde plasiyer'i de temizle
            }

            await InvokeAsync(StateHasChanged);
        }

        protected async Task SetCustomerInfo(int customerId)
        {
            if (Invoice == null) return;

            var customerDetailRs = await CustomerService.GetCustomerById(customerId);
            if (customerDetailRs.Ok && customerDetailRs.Result != null)
            {
                Invoice.AverageMaturity = customerDetailRs.Result.Vade;
                Invoice.RiskLimit = customerDetailRs.Result.RiskLimit;
                Invoice.CustomerName = customerDetailRs.Result.Name;
                selectedCustomerSearch = customerDetailRs.Result.Name;
                
                // CurrentBalance ve LastServiceTotal CustomerAccountTransaction'dan hesaplanmalı
                Invoice.CurrentBalance = 0;
                Invoice.LastServiceTotal = 0;

                CustomerWorkingTypeStr = customerDetailRs.Result.CustomerWorkingType switch
                {
                    CustomerWorkingTypeEnum.Pesin => "Peşin",
                    CustomerWorkingTypeEnum.Vadeli => "Vadeli",
                    CustomerWorkingTypeEnum.PesinAndVadeli => "Peşin & Vadeli",
                    _ => "Belirtilmemiş"
                };
                CustomerMaturity = customerDetailRs.Result.Vade;
            }
        }

        protected void OnWarehouseChangeSync(object value) { _ = InvokeAsync(async () => await OnWarehouseChange(value)); }
        protected async Task OnWarehouseChange(object value)
        {
            if (Invoice == null || value == null) return;
            
            if (value is int warehouseId)
            {
                var wh = WarehouseOptions.FirstOrDefault(w => w.Id == warehouseId);
                if (wh != null)
                {
                    Invoice.WarehouseId = wh.Id;
                    Invoice.Warehouse = wh.Name;
                    Invoice.CorporationId = wh.CorporationId;
                    Invoice.BranchId = wh.BranchId;
                }
            }
            await InvokeAsync(StateHasChanged);
        }

        protected async Task LoadProducts(LoadDataArgs args)
        {
            isProductLoading = true;
            await InvokeAsync(StateHasChanged);
            await Task.Delay(300); // UI Loading göstergesi için kısa bekleme

            try
            {
                var search = args.Filter;
                if (string.IsNullOrWhiteSpace(search) || search.Length < 2)
                {
                    productSearchResults = new List<ProductListDto>();
                    isProductLoading = false;
                    await InvokeAsync(StateHasChanged);
                    return;
                }

                // Fatura modalında BranchId (şube) filtresi zorunlu — sadece ProductService.SearchProducts kullan (tenant’a göre filtreler)
                var rs = await ProductService.SearchProducts(search);
                if (rs.Ok && rs.Result != null)
                {
                    productSearchResults = rs.Result.Take(50).ToList();
                }
                else
                {
                    productSearchResults = new List<ProductListDto>();
                }
                
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, $"Ürün arama hatası: {ex.Message}");
                productSearchResults = new List<ProductListDto>();
            }
            finally
            {
                isProductLoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        protected int CustomerCount;
        protected async Task LoadCustomers(LoadDataArgs args)
        {
            try 
            {
                isCustomerLoading = true;
                await InvokeAsync(StateHasChanged);

                var res = await CustomerService.GetPagedCustomersForInvoice(new PageSetting 
                { 
                    Search = args.Filter, 
                    OrderBy = "Name asc", 
                    Skip = args.Skip ?? 0, 
                    Take = args.Top ?? 50 
                });
                
                if (res.Ok && res.Result?.Data != null)
                {
                    var newList = res.Result.Data.ToList();
                    
                    // Mevcut seçili müşteriyi koruyalım — yeni listede yoksa ekle
                    if (Invoice?.CustomerId.HasValue == true && Invoice.CustomerId > 0)
                    {
                        if (!newList.Any(c => c.Id == Invoice.CustomerId))
                        {
                            var existing = Customers?.FirstOrDefault(c => c.Id == Invoice.CustomerId);
                            if (existing != null)
                            {
                                newList.Insert(0, existing);
                            }
                        }
                    }
                    
                    Customers = newList;
                    CustomerCount = res.Result.DataCount;
                    
                    if (CustomerCount == 0 && Customers.Any())
                    {
                        CustomerCount = Customers.Count;
                    }
                }
                else if (!res.Ok)
                {
                    NotificationService.Notify(NotificationSeverity.Warning, "Cari arama uyarısı", res.Metadata?.Message);
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Cari yüklenirken hata", ex.Message);
            }
            finally
            {
                isCustomerLoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        protected void ClearProductSearch()
        {
            selectedProductCode = string.Empty;
            productSearchResults = new List<ProductListDto>();
            StateHasChanged();
        }

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
                    // selectedProductCode artık Id veya Barcode olabilir. Önce Id'den (IdStr) dene.
                    product = productSearchResults.FirstOrDefault(p => p.IdStr == selectedProductCode);
                    
                    if (product == null)
                    {
                         product = productSearchResults.FirstOrDefault(p => 
                            p.Barcode == selectedProductCode || 
                            p.Name.Contains(selectedProductCode, StringComparison.OrdinalIgnoreCase));
                    }

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

                if (Invoice == null)
                {
                    Invoice = new InvoiceUpsertDto
                    {
                        Items = new List<InvoiceItemUpsertDto>(),
                        BranchId = TenantProvider.GetCurrentBranchId()
                    };
                }

                Invoice.Items ??= new List<InvoiceItemUpsertDto>();

                if (Invoice.Items.Any(i => i.ProductId == product.Id))
                {
                    NotificationService.Notify(NotificationSeverity.Warning, "Bu ürün zaten listede mevcut.");
                    return;
                }

                // Birim verilerini yükle (eğer önbellekte yoksa)
                if (!ProductUnitsCache.ContainsKey(product.Id))
                {
                    var unitsRs = await ProductUnitService.GetProductUnitsByProductId(product.Id);
                    if (unitsRs.Ok && unitsRs.Result != null)
                    {
                        ProductUnitsCache[product.Id] = unitsRs.Result.ToList();
                    }
                }

                // Product'tan ProductUnit ilişkisini kullan - hardcoded değer yok
                string? defaultUnit = null;
                int? defaultUnitId = null;
                decimal defaultQuantity = 1; // ProductUnit'ten gelecek, geçici olarak 1

                if (ProductUnitsCache.TryGetValue(product.Id, out var cachedUnits) && cachedUnits.Any())
                {
                    // Eğer aranan kod bir birim barkodu ise, o birimi ve miktarını seç
                    var matchedUnitByBarcode = cachedUnits.FirstOrDefault(u => u.Barcode == selectedProductCode);
                    if (matchedUnitByBarcode != null)
                    {
                        defaultUnit = matchedUnitByBarcode.UnitName;
                        defaultUnitId = matchedUnitByBarcode.Id;
                        defaultQuantity = matchedUnitByBarcode.UnitValue;
                    }
                    else
                    {
                        // ProductUnit'lerden ilk birimi seç (hardcoded "Adet" veya UnitValue kontrolü yok)
                        var selectedUnit = cachedUnits.First();
                        defaultUnit = selectedUnit.UnitName;
                        defaultUnitId = selectedUnit.Id;
                        defaultQuantity = selectedUnit.UnitValue;
                    }
                }
                else
                {
                    // ProductUnit yoksa hata göster
                    NotificationService.Notify(NotificationSeverity.Error, "Ürün Birimi Bulunamadı", 
                        $"'{product.Name}' ürünü için ProductUnit kaydı bulunamadı. Lütfen ürün birimlerini kontrol edin.");
                    return;
                }

                // Fiyat belirleme: Price 0 ise RetailPrice kullan
                decimal price = product.Price > 0 ? product.Price : (product.RetailPrice ?? 0);
                
                Console.WriteLine($"Product: {product.Name}, Price: {product.Price}, RetailPrice: {product.RetailPrice}, Selected Price: {price}");

                // **VALIDATION: Ürün Kontrolü**
                var validationErrors = new List<string>();
                
                if (string.IsNullOrWhiteSpace(product.Name))
                    validationErrors.Add("• Ürün adı boş olamaz");
                
                if (string.IsNullOrWhiteSpace(defaultUnit))
                    validationErrors.Add("• Birim tanımlı değil");
                
                if (defaultQuantity <= 0)
                    validationErrors.Add("• Miktar 0'dan büyük olmalı");
                
                // Eğer birden fazla validation hatası varsa göster ve çık
                if (validationErrors.Any())
                {
                    var errorMessage = $"Hatalar: {string.Join(", ", validationErrors)}";
                    NotificationService.Notify(NotificationSeverity.Error, "Ürün Hatalı", errorMessage);
                    return;
                }

                // **FİYAT 0 İSE ONAY İSTE**
                if (price <= 0)
                {
                    var confirm = await DialogService.Confirm(
                        $"'{product.Name}' ürününün fiyatı 0,00 olarak görünüyor.\n\nYine de eklemek istiyor musunuz?",
                        "Fiyat Uyarısı",
                        new ConfirmOptions { OkButtonText = "Evet, Ekle", CancelButtonText = "Hayır" });

                    if (confirm != true)
                    {
                        return; // Kullanıcı iptal etti
                    }
                }

                var newItem = new InvoiceItemUpsertDto
                {
                    Order = 1, // En başa eklenecek
                    ProductId = product.Id,
                    ProductCode = product.Barcode,
                    ProductName = product.Name ?? string.Empty,
                    Unit = defaultUnit,
                    ProductUnitId = defaultUnitId,
                    Quantity = defaultQuantity,
                    Price = price,
                    VatRate = product.Kdv ?? 20,
                    IsNew = true,
                    HasError = price <= 0 || string.IsNullOrWhiteSpace(defaultUnit) || (product.Kdv ?? 0) <= 0
                };

                // Diğerlerinin IsNew işaretini kaldır
                foreach (var item in Invoice.Items)
                {
                    item.IsNew = false;
                }

                Invoice.Items.Insert(0, newItem); // Listeye en üstten ekle

                // Sıralamayı güncelle
                for (int i = 0; i < Invoice.Items.Count; i++)
                {
                    Invoice.Items[i].Order = i + 1;
                }
                
                CalculateItemTotal(newItem);

                selectedProductCode = string.Empty;
                productSearchResults.Clear();
                
                if (itemsGrid != null)
                {
                    await itemsGrid.Reload();
                }
                else
                {
                    StateHasChanged();
                }

                RecalculateTotals();

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

        protected void RowRender(RowRenderEventArgs<InvoiceItemUpsertDto> args)
        {
            if (args.Data.HasError)
            {
                args.Attributes.Add("class", "error-item-highlight");
            }
            else if (args.Data.IsNew)
            {
                args.Attributes.Add("class", "new-item-highlight");
            }
        }

        protected void AddInvoiceItem()
        {
            if (Invoice != null)
            {
                Invoice.Items.Add(new InvoiceItemUpsertDto
                {
                    Order = Invoice.Items.Count + 1,
                    Quantity = 1,
                    VatRate = 20
                });
            }
        }

        protected void RemoveInvoiceItemSync(InvoiceItemUpsertDto item) { _ = InvokeAsync(async () => await RemoveInvoiceItem(item)); }
        protected async Task RemoveInvoiceItem(InvoiceItemUpsertDto item)
        {
            try
            {
                if (Invoice == null || Invoice.Items == null)
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
                    Invoice.Items.Remove(item);
                    
                    for (int i = 0; i < Invoice.Items.Count; i++)
                    {
                        Invoice.Items[i].Order = i + 1;
                    }

                    if (itemsGrid != null)
                    {
                        await itemsGrid.Reload();
                    }
                    else
                    {
                        StateHasChanged();
                    }

                    RecalculateTotals();

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

        protected void CalculateItemTotal(InvoiceItemUpsertDto item)
        {
            // Just a wrapper for the unified recalculation logic
            RecalculateTotals();
        }

        protected void OnUnitChange(object value, InvoiceItemUpsertDto item)
        {
            if (value is int unitId && item.ProductId.HasValue)
            {
                if (ProductUnitsCache.TryGetValue(item.ProductId.Value, out var units))
                {
                    var newUnit = units.FirstOrDefault(u => u.Id == unitId);
                    var oldUnit = units.FirstOrDefault(u => u.Id == item.ProductUnitId);
                    
                    if (newUnit != null)
                    {
                        // Birim değiştiğinde fiyatı UnitValue'ya göre dönüştür
                        if (oldUnit != null && oldUnit.UnitValue > 0 && newUnit.UnitValue > 0)
                        {
                            decimal conversionFactor = newUnit.UnitValue / oldUnit.UnitValue;
                            item.Price = item.Price * conversionFactor;
                            item.PriceCurrency = item.PriceCurrency * conversionFactor;
                        }
                        
                        item.ProductUnitId = newUnit.Id;
                        item.Unit = newUnit.UnitName; // Görüntüleme için adını da tutuyoruz
                        CalculateItemTotal(item);
                    }
                }
            }
            else
            {
                item.ProductUnitId = null;
                item.Unit = string.Empty;
                CalculateItemTotal(item);
            }
        }

        // Helper methods for calculations
        protected void OnRecalculateTotalsDecimal(decimal args) => RecalculateTotals();
        protected void OnRecalculateTotalsBool(bool args) => RecalculateTotals();
        
        protected void OnCalculateItemTotalDecimal(decimal args, InvoiceItemUpsertDto item) 
        {
            item.Quantity = args;
            CalculateItemTotal(item);
        }

        protected void OnCalculateItemTotalPrice(decimal args, InvoiceItemUpsertDto item) 
        {
            item.Price = args;
            CalculateItemTotal(item);
        }

        protected void OnCalculateItemTotalDiscount(decimal args, InvoiceItemUpsertDto item) 
        {
            item.Discount1 = args;
            CalculateItemTotal(item);
        }

        protected void OnCalculateItemTotalDecimalNullable(decimal? args, InvoiceItemUpsertDto item) => CalculateItemTotal(item);
        protected void OnRecalculateTotalsDecimal(object args) => RecalculateTotals();

        protected void RecalculateTotals()
        {
            if (Invoice == null) return;

            decimal rate = Invoice.ExchangeRate > 0 ? Invoice.ExchangeRate : 1;
            decimal totalAmountCurrency = 0; 
            decimal totalDiscountCurrency = 0;
            decimal totalVatCurrency = 0;
            var vatByRate = new Dictionary<decimal, decimal>();

            foreach (var item in Invoice.Items)
            {
                // Item level calculations
                item.PriceCurrency = item.Price;
                decimal vatRate = item.VatRate / 100m;
                
                // 1. Determine Net Unit Price for calculation base
                decimal unitPriceNet = item.Price;
                if (Invoice.IsVatIncluded)
                {
                    unitPriceNet = item.Price / (1 + vatRate); // Don't round yet for better precision
                }

                // 2. Line Gross (Qty * Net Price) before discount
                decimal lineGross = item.Quantity * unitPriceNet;
                totalAmountCurrency += Math.Round(lineGross, 2);

                // 3. Line Discount
                decimal lineDiscount = 0;
                if (item.Discount1 > 0)
                {
                    lineDiscount = lineGross * (item.Discount1 / 100m);
                }
                totalDiscountCurrency += Math.Round(lineDiscount, 2);

                // 4. Line Net (Gross - Discount)
                item.TotalCurrency = Math.Round(lineGross - lineDiscount, 2);
                item.Total = Math.Round(item.TotalCurrency * (IsTry ? 1m : rate), 2);

                // 5. Line VAT (Net * Rate) ve oran bazında biriktir
                decimal lineVat = item.TotalCurrency * vatRate;
                decimal lineVatRounded = Math.Round(lineVat, 2);
                totalVatCurrency += lineVatRounded;
                decimal itemRate = item.VatRate;
                if (!vatByRate.ContainsKey(itemRate)) vatByRate[itemRate] = 0;
                vatByRate[itemRate] += lineVatRounded;
                
                // Validation
                item.HasError = item.Price <= 0 || item.Quantity <= 0;
            }

            VatAmountsByRate = vatByRate.Select(kv => (kv.Key, Math.Round(kv.Value, 2))).OrderBy(x => x.Key).ToList();

            // Global Totals (Currency)
            Invoice.TotalAmountCurrency = Math.Round(totalAmountCurrency, 2);
            Invoice.DiscountTotalCurrency = Math.Round(totalDiscountCurrency, 2);
            Invoice.VatTotalCurrency = Math.Round(totalVatCurrency, 2);
            Invoice.GeneralTotalCurrency = Math.Round((totalAmountCurrency - totalDiscountCurrency) + totalVatCurrency, 2);

            // Global Totals (Local Try)
            decimal tryConv = IsTry ? 1m : rate;
            Invoice.TotalAmount = Math.Round(Invoice.TotalAmountCurrency * tryConv, 2);
            Invoice.DiscountTotal = Math.Round(Invoice.DiscountTotalCurrency * tryConv, 2);
            Invoice.VatTotal = Math.Round(Invoice.VatTotalCurrency * tryConv, 2);
            Invoice.GeneralTotal = Math.Round(Invoice.GeneralTotalCurrency * tryConv, 2);

            _ = InvokeAsync(StateHasChanged);
        }
        

        // Fallback birim listesi: ProductUnitsCache'de ürün yoksa "Adet" (Id=2) döndür
        private static readonly List<ProductUnitListDto> _fallbackUnitList = new()
        {
            new ProductUnitListDto { Id = 2, UnitName = "Adet", UnitValue = 1 }
        };

        protected IEnumerable<ProductUnitListDto> GetProductUnits(int? productId)
        {
            if (productId.HasValue && ProductUnitsCache.TryGetValue(productId.Value, out var units) && units.Any())
            {
                return units;
            }
            // Fallback: Cache'de birim yoksa "Adet" (Id=2) döndür
            return new List<ProductUnitListDto>
            {
                new ProductUnitListDto { Id = 2, UnitName = "Adet", UnitValue = 1, ProductId = productId ?? 0 }
            };
        }

        /// <summary>
        /// Kalem için birim listesi; ProductUnitId listede yoksa (silinmiş birim vb.) item.Unit ile sentetik seçenek eklenir.
        /// Böylece düzenlemede BİRİM dropdown'ı boş görünmez.
        /// </summary>
        protected IEnumerable<ProductUnitListDto> GetProductUnitsForItem(InvoiceItemUpsertDto item)
        {
            var list = GetProductUnits(item.ProductId).ToList();
            if (item.ProductUnitId.HasValue && list.All(u => u.Id != item.ProductUnitId.Value) && !string.IsNullOrWhiteSpace(item.Unit))
            {
                list = new List<ProductUnitListDto>(list)
                {
                    new ProductUnitListDto { Id = item.ProductUnitId.Value, UnitName = item.Unit!.Trim(), UnitValue = 1, ProductId = item.ProductId ?? 0 }
                };
            }
            return list;
        }

        protected void FormSubmitSync(InvoiceUpsertDto args) { _ = InvokeAsync(async () => await FormSubmit(args)); }
        protected async Task FormSubmit(InvoiceUpsertDto args)
        {
            try
            {
                if (Invoice == null) return;
                
                _logger.LogInformation("[FormSubmit] Starting. ID: {Id}, InvoiceNo: {InvoiceNo}, CustomerId: {CustomerId}, CustomerName: {CustomerName}, WarehouseId: {WarehouseId}, Warehouse: {Warehouse}, CurrencyId: {CurrencyId}, ExchangeRate: {ExchangeRate}, SalesPersonId: {SalesPersonId}, CorporationId: {CorpId}, BranchId: {BranchId}, Total: {Total}", 
                    Invoice.Id, Invoice.InvoiceNo, Invoice.CustomerId, Invoice.CustomerName, Invoice.WarehouseId, Invoice.Warehouse, Invoice.CurrencyId, Invoice.ExchangeRate, Invoice.SalesPersonId, Invoice.CorporationId, Invoice.BranchId, Invoice.GeneralTotal);

                if (Invoice.BranchId == 0)
                    Invoice.BranchId = TenantProvider.GetCurrentBranchId();

                if (Invoice.CustomerId == null || Invoice.CustomerId == 0)
            {
                NotificationService.Notify(NotificationSeverity.Warning, "Lütfen bir Cari (Müşteri) seçiniz.");
                return;
            }

            Saving = true;
                
                // **HEADER VALIDATION: Zorunlu alanlar kontrolü**
                var headerValidationErrors = new List<string>();
                
                if (!args.InvoiceTypeId.HasValue || args.InvoiceTypeId.Value <= 0)
                    headerValidationErrors.Add("Fatura Tipi seçmelisiniz");
                
                if (!args.CustomerId.HasValue || args.CustomerId.Value <= 0)
                    headerValidationErrors.Add("Cari seçmelisiniz");
                
                if (!args.PaymentTypeId.HasValue || args.PaymentTypeId.Value <= 0)
                    headerValidationErrors.Add("Ödeme Tipi seçmelisiniz");
                
                if (!args.WarehouseId.HasValue || args.WarehouseId.Value <= 0)
                    headerValidationErrors.Add("Depo seçmelisiniz");
                
                if (!args.CurrencyId.HasValue || args.CurrencyId.Value <= 0)
                    headerValidationErrors.Add("Döviz Tipi seçmelisiniz");
                
                if (string.IsNullOrWhiteSpace(args.InvoiceNo))
                    headerValidationErrors.Add("Fatura No girmelisiniz");
                
                if (string.IsNullOrWhiteSpace(args.InvoiceSerialNo))
                    headerValidationErrors.Add("Seri No girmelisiniz");
                
                if (headerValidationErrors.Any())
                {
                    var errorMessage = "Lütfen aşağıdaki alanları doldurun:\n" + string.Join("\n", headerValidationErrors.Select(e => $"• {e}"));
                    NotificationService.Notify(new NotificationMessage 
                    { 
                        Severity = NotificationSeverity.Error, 
                        Summary = "Eksik Bilgiler", 
                        Detail = errorMessage,
                        Duration = 10000
                    });
                    Saving = false;
                    return;
                }
                
                // **KAPSAMLI VALIDASYON: Fiyat, Birim, Miktar, KDV**
                var itemValidationErrors = new List<(InvoiceItemUpsertDto Item, List<string> Errors)>();
                
                // Invoice.Items üzerinden doğrula (grid ile tam senkronize liste)
                foreach (var item in Invoice.Items)
                {
                    var errors = new List<string>();
                    
                    if (item.Price <= 0)
                        errors.Add("Fiyat girmelisiniz");
                    
                    if (string.IsNullOrWhiteSpace(item.Unit))
                        errors.Add("Birim seçmelisiniz");
                    
                    if (item.Quantity <= 0)
                        errors.Add("Miktar girmelisiniz");
                    
                    if (item.VatRate <= 0)
                        errors.Add("KDV girmelisiniz");
                    
                    if (errors.Any())
                    {
                        item.HasError = true;
                        itemValidationErrors.Add((item, errors));
                    }
                    else
                    {
                        item.HasError = false;
                    }
                }
                
                if (itemValidationErrors.Any())
                {
                    await itemsGrid?.Reload()!;
                    StateHasChanged();
                    
                    var errorMessage = "Düzeltilmesi gereken hatalar var:\n";
                    foreach (var (item, errors) in itemValidationErrors)
                    {
                        errorMessage += $"• {item.ProductName}: {string.Join(", ", errors)}\n";
                    }
                    
                    NotificationService.Notify(new NotificationMessage 
                    { 
                        Severity = NotificationSeverity.Error, 
                        Summary = "Kayıt Durduruldu", 
                        Detail = errorMessage,
                        Duration = 10000
                    });
                    
                    Saving = false;
                    return;
                }
                
                var response = await InvoiceService.UpsertInvoice(new AuditWrapDto<InvoiceUpsertDto>
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
                        Detail = "Fatura kaydedildi."
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

        private async Task LoadInvoiceFromOrders(List<int> orderIds)
        {
            if (_isProcessingOrders || orderIds == null || !orderIds.Any()) return;
            
            try
            {
                _isProcessingOrders = true;
                var distinctIds = orderIds.Distinct().ToList();

                // Initialize a clean Invoice object
                Invoice = new InvoiceUpsertDto
                {
                    OrderId = distinctIds.First(),
                    OrderIds = distinctIds,
                    InvoiceDate = DateTime.Now,
                    InvoiceNo = GenerateInvoiceNo(),
                    InvoiceSerialNo = "X",
                    IsVatIncluded = true,
                    ExchangeRate = 1,
                    BranchId = TenantProvider.GetCurrentBranchId(),
                    Items = new List<InvoiceItemUpsertDto>()
                };

                // Default to Sales Invoice
                if (InvoiceTypes != null && InvoiceTypes.Any())
                {
                    Invoice.InvoiceTypeId = InvoiceTypes.FirstOrDefault(t => t.Name.Contains("Satış"))?.Id ?? InvoiceTypes.First().Id;
                }

                // Default to TRY currency
                if (Currencies != null && Currencies.Any())
                {
                    var tryCurrency = Currencies.FirstOrDefault(c => c.CurrencyCode == "TRY") ?? Currencies.FirstOrDefault();
                    if (tryCurrency != null)
                    {
                        Invoice.CurrencyId = tryCurrency.Id;
                        Invoice.CurrencyCode = tryCurrency.CurrencyCode;
                        Invoice.ExchangeRate = 1;
                    }
                }

                var resultSet = await OrderService.GetOrdersByIds(distinctIds);
                if (resultSet.Ok && resultSet.Result != null)
                {
                    var uniqueOrders = resultSet.Result
                        .GroupBy(o => o.Id)
                        .Select(g => g.First())
                        .ToList();

                    if (!uniqueOrders.Any()) return;

                    // DEBUG: GetOrdersByIds'den dönen OrderItems durumunu logla
                    foreach (var uo in uniqueOrders)
                    {
                        Console.WriteLine($"[LoadInvoiceFromOrders] GetOrdersByIds sonrası — Order #{uo.Id} ({uo.OrderNumber}): OrderItems count = {uo.OrderItems?.Count ?? -1}");
                    }

                    // ÖNEMLİ: OrderItems'ı ayrı sorgu ile doğrudan çek — Include/AutoMapper sorunlarını bypass eder
                    var directItems = await OrderService.GetOrderItemsDirectByOrderIds(distinctIds);
                    Console.WriteLine($"[LoadInvoiceFromOrders] directItems count: {directItems.Count}, orderIds: {string.Join(",", distinctIds)}");
                    _logger.LogWarning("[LoadInvoiceFromOrders] directItems count: {Count}, orderIds: {Ids}", directItems.Count, string.Join(",", distinctIds));
                    if (directItems.Any())
                    {
                        // Her siparişe kendi ürünlerini ata
                        foreach (var ord in uniqueOrders)
                        {
                            var itemsForOrder = directItems.Where(i => i.OrderId == ord.Id).ToList();
                            if (itemsForOrder.Any())
                            {
                                ord.OrderItems = itemsForOrder;
                            }
                        }
                    }
                    
                    // Accordion için sipariş detaylarını oluştur
                    _orderAccordionItems = uniqueOrders.Select(o => new OrderAccordionItem
                    {
                        OrderId = o.Id,
                        OrderNumber = o.OrderNumber ?? $"#{o.Id}",
                        CustomerName = o.CustomerName ?? "Bilinmiyor",
                        CreatedDate = o.CreatedDate,
                        OrderTotal = o.GrandTotal,
                        CargoPrice = o.CargoPrice,
                        StatusText = o.OrderStatusType.ToString(),
                        Items = (o.OrderItems ?? new List<OrderItems>())
                            .Where(oi => oi.ProductId > 0 || !string.IsNullOrEmpty(oi.ProductName))
                            .Select(oi => new OrderAccordionLineItem
                            {
                                ProductId = oi.ProductId,
                                ProductName = oi.ProductName ?? "Bilinmeyen Ürün",
                                Quantity = oi.Quantity,
                                Price = oi.Price,
                                TotalPrice = oi.TotalPrice,
                                DiscountAmount = oi.DiscountAmount ?? 0
                            }).ToList()
                    }).ToList();
                    
                    Console.WriteLine($"[LoadInvoiceFromOrders] Accordion oluşturuldu — sipariş sayısı: {_orderAccordionItems.Count}, toplam ürün: {_orderAccordionItems.Sum(o => o.Items.Count)}");
                    foreach (var acc in _orderAccordionItems)
                    {
                        Console.WriteLine($"  Sipariş #{acc.OrderNumber}: {acc.Items.Count} ürün, OrderItems null mu: {(uniqueOrders.FirstOrDefault(o => o.Id == acc.OrderId)?.OrderItems == null)}");
                    }

                    var firstOrder = uniqueOrders.First();
                    Invoice.CustomerId = firstOrder.CustomerId;
                    Invoice.WarehouseId = firstOrder.SellerId;
                    
                    // Populate CorporationId and BranchId from warehouse selection logic
                    if (Invoice.WarehouseId.HasValue)
                    {
                        await OnWarehouseChange(Invoice.WarehouseId.Value);
                    }
                    if (Invoice.BranchId == 0)
                        Invoice.BranchId = TenantProvider.GetCurrentBranchId();

                    Invoice.IsVatIncluded = true; // B2B orders generally include VAT in first price
                    Invoice.Description = $"Siparişler: {string.Join(", ", uniqueOrders.Select(o => o.OrderNumber))} aktarıldı.";

                    // Cari bilgisini set et — önce servis, başarısız olursa sipariş verisinden fallback
                    if (Invoice.CustomerId.HasValue && Invoice.CustomerId > 0)
                    {
                        var list = (Customers ?? new List<CustomerListDto>()).ToList();
                        var existingCustomer = list.FirstOrDefault(c => c.Id == Invoice.CustomerId);
                        
                        if (existingCustomer == null)
                        {
                            var custRs = await CustomerService.GetCustomerById(Invoice.CustomerId.Value);
                            if (custRs.Ok && custRs.Result != null)
                            {
                                list.Add(new CustomerListDto 
                                { 
                                    Id = (int)custRs.Result.Id, 
                                    Name = custRs.Result.Name,
                                    Code = custRs.Result.Code,
                                    Phone = custRs.Result.Phone
                                });
                                Customers = list;
                            }
                            else if (!string.IsNullOrEmpty(firstOrder.CustomerName))
                            {
                                // GetCustomerById başarısız (BranchId filtresi vb.) — siparişten fallback
                                list.Add(new CustomerListDto 
                                { 
                                    Id = Invoice.CustomerId.Value, 
                                    Name = firstOrder.CustomerName,
                                    Code = "",
                                    Phone = ""
                                });
                                Customers = list;
                            }
                        }
                        
                        // OnCustomerChange yerine doğrudan set et — daha güvenilir
                        var matchedCustomer = Customers.FirstOrDefault(c => c.Id == Invoice.CustomerId);
                        if (matchedCustomer != null)
                        {
                            Invoice.CustomerName = matchedCustomer.Name;
                            selectedCustomerSearch = matchedCustomer.Name;
                            await SetCustomerInfo(matchedCustomer.Id);
                        }
                        else
                        {
                            // Son çare: siparişteki adı kullan
                            Invoice.CustomerName = firstOrder.CustomerName;
                            selectedCustomerSearch = firstOrder.CustomerName;
                        }
                    }

                    // FIX: Payment Type
                    if (firstOrder.PaymentTypeId > 0)
                    {
                        Invoice.PaymentTypeId = (int)firstOrder.PaymentTypeId;
                        await OnPaymentTypeChange(Invoice.PaymentTypeId.Value);
                    }

                    // PRE-FETCH UNITS
                    var allOrderProductIds = uniqueOrders
                        .SelectMany(o => o.OrderItems)
                        .Where(x => x.ProductId > 0)
                        .Select(x => x.ProductId)
                        .Distinct()
                        .ToList();

                    if (allOrderProductIds.Any())
                    {
                        var unitRs = await ProductUnitService.GetProductUnitsByProductIds(allOrderProductIds);
                        if (unitRs.Ok && unitRs.Result != null)
                        {
                            foreach (var pid in allOrderProductIds)
                                ProductUnitsCache[pid] = unitRs.Result.Where(u => u.ProductId == pid).ToList();
                        }
                    }

                    int itemOrder = 1;
                    var consolidatedList = new List<InvoiceItemUpsertDto>();
                    var processedItemIds = new HashSet<int>();

                    // Explicitly iterate distinct orders to avoid Cartesian issues
                    foreach (var order in uniqueOrders)
                    {
                        if (order.OrderItems == null) continue;

                        // FIXED: Group by signature (Product+Price+Qty) to eliminate duplicate DB rows
                        var nonDuplicateItems = order.OrderItems
                            .Where(x => x.Id > 0)
                            .GroupBy(x => new { x.ProductId, x.Quantity, x.TotalPrice }) 
                            .Select(g => g.First());

                        foreach (var orderItem in nonDuplicateItems)
                        {
                            if (processedItemIds.Contains(orderItem.Id)) continue; // SKIP DUPLICATES
                            processedItemIds.Add(orderItem.Id);

                            if (orderItem.ProductId == 0 && string.IsNullOrEmpty(orderItem.ProductName)) continue;

                            decimal qty = orderItem.Quantity > 0 ? (decimal)orderItem.Quantity : 1m;
                            
                            // B2B Precision Fix: Always use (TotalPrice / Qty) to get the actual net unit price
                            decimal unitPrice = Math.Round(orderItem.TotalPrice / qty, 4);

                            var existing = consolidatedList.FirstOrDefault(i => i.ProductId == orderItem.ProductId && Math.Abs(i.Price - unitPrice) < 0.001m);
                            
                            if (existing != null)
                            {
                                existing.Quantity += qty;
                            }
                            else
                            {
                                // find unit id
                                int? unitId = null;
                                string unitName = "Adet";

                                if (ProductUnitsCache.TryGetValue(orderItem.ProductId, out var units) && units.Any())
                                {
                                    // Önce "Adet" birimini ara, yoksa UnitValue=1 olanı, yoksa ilkini al
                                    var u = units.FirstOrDefault(x => x.UnitName == "Adet") ?? units.FirstOrDefault(x => x.UnitValue == 1) ?? units.FirstOrDefault();
                                    if (u != null) 
                                    {
                                        unitId = u.Id;
                                        unitName = u.UnitName;
                                    }
                                }

                                // Fallback: Birim bulunamadıysa "Adet" (Id=2) ata
                                unitId ??= 2;

                                consolidatedList.Add(new InvoiceItemUpsertDto
                                {
                                    ProductId = orderItem.ProductId,
                                    ProductCode = orderItem.Product?.Barcode ?? "",
                                    ProductName = orderItem.ProductName ?? orderItem.Product?.Name ?? "Bilinmeyen Ürün",
                                    Unit = unitName,
                                    ProductUnitId = unitId,
                                    Quantity = qty,
                                    Price = unitPrice,
                                    PriceCurrency = unitPrice,
                                    Discount1 = 0,
                                    VatRate = orderItem.Product?.Tax?.TaxRate ?? 20,
                                    Order = itemOrder++
                                });
                            }
                        }
                    }

                    // ADD CARGO AS LINE ITEM
                    var totalCargo = uniqueOrders.Sum(o => o.CargoPrice);
                    if (totalCargo > 0)
                    {
                        consolidatedList.Add(new InvoiceItemUpsertDto
                        {
                            ProductName = "Kargo Ücreti",
                            Unit = "Adet",
                            Quantity = 1,
                            Price = totalCargo,
                            PriceCurrency = totalCargo,
                            VatRate = 20,
                            Order = itemOrder++
                        });
                    }

                    // ADD ORDER-LEVEL DISCOUNTS AS LINE ITEM (Global discounts like Coupons)
                    var totalOrderDiscounts = uniqueOrders.Sum(o => o.DiscountTotal ?? 0);
                    var totalItemDiscounts = uniqueOrders.SelectMany(o => o.OrderItems).Sum(oi => (oi.DiscountAmount ?? 0));
                    var globalDiscountDiff = totalOrderDiscounts - totalItemDiscounts;
                    
                    if (globalDiscountDiff > 0)
                    {
                         consolidatedList.Add(new InvoiceItemUpsertDto
                        {
                            ProductName = "Genel İskonto",
                            Unit = "Adet",
                            Quantity = 1,
                            Price = -globalDiscountDiff, // Negative price for deduction
                            PriceCurrency = -globalDiscountDiff,
                            VatRate = 20,
                            Order = itemOrder++
                        });
                    }

                    Invoice.Items = consolidatedList;
                    
                    // Removed redundant late unit fetching logic since it's done upfront now.

                    RecalculateTotals();
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, $"Import Hatası: {ex.Message}");
            }
            finally
            {
                _isProcessingOrders = false;
                await InvokeAsync(StateHasChanged);
                if (itemsGrid != null) await itemsGrid.Reload();
            }
        }

    }
}
