using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using ecommerce.Admin.Domain.Dtos.OrderDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Utils;
using ecommerce.Core.Extensions;
using ecommerce.Core.Entities;
using Radzen;
using ecommerce.Web.Domain.Services.Abstract;

namespace ecommerce.Admin.Components.Pages.B2B
{
    public partial class MyOrders : IDisposable
    {
        [Inject] protected NavigationManager NavigationManager { get; set; } = null!;
        [Inject] protected IOrderService OrderService { get; set; } = null!;
        [Inject] protected AuthenticationService Security { get; set; } = null!;
        [Inject] protected NotificationService NotificationService { get; set; } = null!;
        [Inject] protected DialogService DialogService { get; set; } = null!;
        [Inject] protected TooltipService TooltipService { get; set; } = null!;
        [Inject] protected Microsoft.Extensions.Configuration.IConfiguration Configuration { get; set; } = null!;
        [Inject] protected ecommerce.Odaksodt.Abstract.IOdaksoftInvoiceService OdaksoftInvoiceService { get; set; } = null!;

        private List<OrderListDto>? orders = new();
        private List<OrderListDto>? filteredOrders = new();
        private bool isLoading = true;
        private HashSet<int> expandedOrderIds = new(); // Track which orders are expanded
        private bool isCancelling = false;
        private int? cancellingOrderId = null;
        
        // Pagination
        private int currentPage = 1;
        private int pageSize = 5;
        private int totalPages = 1;
        
        // Filter
        private OrderStatusType? selectedStatusFilter = null; // null = "Hepsi"
        private bool isCargoFilterActive = false; // "Kargodaki" tabı için özel flag
        private string searchOrderNumber = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                // Check if user is authenticated
                if (Security.User == null)
                {
                    NavigationManager.NavigateTo("/login");
                    return;
                }

                // Check for search query parameter
                var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
                var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
                if (queryParams.TryGetValue("search", out var searchValue))
                {
                    searchOrderNumber = searchValue.ToString().Trim();
                }

                await LoadOrders();
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Hata",
                    Detail = "Siparişler yüklenirken bir hata oluştu.",
                    Duration = 4000
                });
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task LoadOrders()
        {
            try
            {
                isLoading = true;
                StateHasChanged();

                ecommerce.Core.Utils.ResultSet.IActionResult<List<OrderListDto>> response;
                
                if (Security.SelectedCustomerId.HasValue)
                {
                    response = await OrderService.GetCustomerOrders(Security.SelectedCustomerId.Value);
                }
                else if (Security.User?.SalesPersonId.HasValue == true)
                {
                   // Plasiyer with no customer selected -> Show ALL linked customers' orders
                   response = await OrderService.GetPlasiyerCustomersOrders(Security.User.Id);
                }
                else
                {
                    response = await OrderService.GetMyOrders();
                }
                
                if (response.Ok && response.Result != null)
                {
                    orders = response.Result;
                    ApplyFilterAndPagination();
                }
                else
                {
                    orders = new List<OrderListDto>();
                    if (!response.Ok)
                    {
                        var errorMessage = response.Metadata?.Message ?? "Siparişler yüklenirken bir hata oluştu.";
                        NotificationService.Notify(new NotificationMessage
                        {
                            Severity = NotificationSeverity.Warning,
                            Summary = "Uyarı",
                            Detail = errorMessage,
                            Duration = 4000
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Hata",
                    Detail = $"Siparişler yüklenirken hata: {ex.Message}",
                    Duration = 4000
                });
                orders = new List<OrderListDto>();
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private void ToggleOrder(int orderId)
        {
            if (expandedOrderIds.Contains(orderId))
                expandedOrderIds.Remove(orderId);
            else
                expandedOrderIds.Add(orderId);
            
            StateHasChanged();
        }

        private bool IsOrderExpanded(int orderId) => expandedOrderIds.Contains(orderId);

        private async Task CancelOrder(OrderListDto order)
        {
            // Sadece "Yeni" durumundaki siparişler iptal edilebilir
            if (order.OrderStatusType != OrderStatusType.OrderNew)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Uyarı",
                    Detail = "Bu sipariş artık iptal edilemez. Sadece yeni siparişler iptal edilebilir.",
                    Duration = 4000
                });
                return;
            }
            
            // Eğer OrderItems içinde CargoTrackUrl dolu olan item varsa, iptal edilemez (kargodaki)
            if (order.OrderItems != null && order.OrderItems.Any(item => !string.IsNullOrEmpty(item.CargoTrackUrl)))
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Uyarı",
                    Detail = "Bu sipariş kargoya verilmiş, iptal edilemez.",
                    Duration = 4000
                });
                return;
            }

            var confirm = await DialogService.Confirm(
                $"Sipariş No: {order.OrderNumber}\n\nToplam Tutar: ₺{order.GrandTotal:N2}\n\nBu siparişi iptal etmek istediğinize emin misiniz?",
                "Sipariş İptal",
                new ConfirmOptions 
                { 
                    OkButtonText = "Evet, İptal Et", 
                    CancelButtonText = "Hayır"
                });

            if (confirm == true)
            {
                try
                {
                    isCancelling = true;
                    cancellingOrderId = order.Id;
                    StateHasChanged();

                    var (success, message) = await OrderService.CancelMyOrder(order.Id);

                    if (success)
                    {
                        NotificationService.Notify(new NotificationMessage
                        {
                            Severity = NotificationSeverity.Success,
                            Summary = "Başarılı",
                            Detail = message,
                            Duration = 4000
                        });

                        // Refresh orders
                        await LoadOrders();
                    }
                    else
                    {
                        NotificationService.Notify(new NotificationMessage
                        {
                            Severity = NotificationSeverity.Error,
                            Summary = "Hata",
                            Detail = message,
                            Duration = 4000
                        });
                    }
                }
                catch (Exception ex)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Hata",
                        Detail = $"Sipariş iptal edilirken hata: {ex.Message}",
                        Duration = 4000
                    });
                }
                finally
                {
                    isCancelling = false;
                    cancellingOrderId = null;
                    StateHasChanged();
                }
            }
        }

        private bool CanCancelOrder(OrderListDto order)
        {
            // Sadece "Yeni" durumundaki siparişler iptal edilebilir
            if (order.OrderStatusType != OrderStatusType.OrderNew)
            {
                return false;
            }
            
            // Eğer OrderItems içinde CargoTrackUrl dolu olan item varsa, iptal edilemez (kargodaki)
            if (order.OrderItems != null && order.OrderItems.Any(item => !string.IsNullOrEmpty(item.CargoTrackUrl)))
            {
                return false;
            }
            
            return true;
        }

        private string GetStatusBadgeClass(OrderStatusType status)
        {
            return status switch
            {
                OrderStatusType.OrderNew => "badge bg-success",
                OrderStatusType.OrderWaitingPayment => "badge bg-warning text-dark",
                OrderStatusType.OrderPrepare => "badge bg-info",
                OrderStatusType.OrderinCargo => "badge bg-primary",
                OrderStatusType.OrderSuccess => "badge bg-success",
                OrderStatusType.OrderCanceled => "badge bg-danger", // Kırmızı - İptal
                OrderStatusType.OrderProblem => "badge bg-danger", // Kırmızı - Sorunlu
                OrderStatusType.OrderWaitingApproval => "badge bg-warning text-dark",
                OrderStatusType.PaymentSuccess => "badge bg-success",
                _ => "badge bg-secondary"
            };
        }

        private string GetStatusText(OrderStatusType status)
        {
            return status.GetDisplayName();
        }

        private string GetPaymentStatusText(OrderListDto order)
        {
            // PaymentTypeId'ye göre ödeme durumunu göster
            if (order.PaymentTypeId == PaymentType.CreditCart)
            {
                return "Sanal Pos Ödendi";
            }
            else if (order.PaymentTypeId == PaymentType.CustomerBalance)
            {
                return "Cari Hesap";
            }
            
            // Fallback: Eski mantık (PaymentStatus bool değerine göre)
            return order.PaymentStatus ? "Ödendi" : "Ödenmedi";
        }

        private string GetPaymentStatusClass(OrderListDto order)
        {
            // PaymentTypeId'ye göre class belirle
            if (order.PaymentTypeId == PaymentType.CreditCart || order.PaymentTypeId == PaymentType.CustomerBalance)
            {
                return "text-success";
            }
            
            // Fallback: Eski mantık
            return order.PaymentStatus ? "text-success" : "text-danger";
        }
        
        private class StatusFilterOption
        {
            public string Text { get; set; } = "";
            public OrderStatusType? Value { get; set; }
        }
        
        private class StatusCountBadge
        {
            public OrderStatusType Status { get; set; }
            public string Label { get; set; } = "";
            public int Count { get; set; }
            public string IconClass { get; set; } = "";
            public bool IsCargoTrackFilter { get; set; } = false; // "Kargodaki" tabı için özel flag
        }
        
        private List<StatusFilterOption> GetStatusFilterOptions()
        {
            return new List<StatusFilterOption>
            {
                new StatusFilterOption { Text = "Hepsi", Value = null },
                new StatusFilterOption { Text = "Yeni Sipariş", Value = OrderStatusType.OrderNew },
                new StatusFilterOption { Text = "Ödeme Bekliyor", Value = OrderStatusType.OrderWaitingPayment },
                new StatusFilterOption { Text = "Hazırlanıyor", Value = OrderStatusType.OrderPrepare },
                new StatusFilterOption { Text = "Kargoda", Value = OrderStatusType.OrderinCargo },
                new StatusFilterOption { Text = "Tamamlananlar", Value = OrderStatusType.OrderSuccess },
                new StatusFilterOption { Text = "İptaller", Value = OrderStatusType.OrderCanceled },
                new StatusFilterOption { Text = "Sorunlu", Value = OrderStatusType.OrderProblem }
            };
        }
        
        private List<StatusCountBadge> GetStatusCountBadges()
        {
            if (orders == null || !orders.Any())
                return new List<StatusCountBadge>();

            var counts = orders
                .GroupBy(o => o.OrderStatusType)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionary(x => x.Status, x => x.Count);

            // "Kargodaki" tabı için: OrderItems içinde CargoTrackUrl dolu olan siparişleri say
            var cargoTrackCount = orders.Count(o => 
                o.OrderItems != null && 
                o.OrderItems.Any(item => !string.IsNullOrEmpty(item.CargoTrackUrl)));

            var badges = new List<StatusCountBadge>
            {
                new StatusCountBadge 
                { 
                    Status = OrderStatusType.OrderNew, 
                    Label = "Yeni", 
                    Count = counts.GetValueOrDefault(OrderStatusType.OrderNew, 0),
                    IconClass = "fas fa-clock"
                },
                new StatusCountBadge 
                { 
                    Status = OrderStatusType.OrderWaitingPayment, 
                    Label = "Ödeme Bekliyor", 
                    Count = counts.GetValueOrDefault(OrderStatusType.OrderWaitingPayment, 0),
                    IconClass = "fas fa-credit-card"
                },
                new StatusCountBadge 
                { 
                    Status = OrderStatusType.OrderPrepare, 
                    Label = "Hazırlanıyor", 
                    Count = counts.GetValueOrDefault(OrderStatusType.OrderPrepare, 0),
                    IconClass = "fas fa-box"
                },
                new StatusCountBadge 
                { 
                    Status = OrderStatusType.OrderinCargo, 
                    Label = "Kargoda", 
                    Count = counts.GetValueOrDefault(OrderStatusType.OrderinCargo, 0),
                    IconClass = "fas fa-truck"
                },
                new StatusCountBadge 
                { 
                    Status = OrderStatusType.OrderSuccess, 
                    Label = "Teslim Edildi", 
                    Count = counts.GetValueOrDefault(OrderStatusType.OrderSuccess, 0),
                    IconClass = "fas fa-check-circle"
                },
                new StatusCountBadge 
                { 
                    Status = OrderStatusType.OrderCanceled, 
                    Label = "İptal", 
                    Count = counts.GetValueOrDefault(OrderStatusType.OrderCanceled, 0),
                    IconClass = "fas fa-times-circle"
                }
            };

            // "Kargodaki" tabını ekle (OrderItems içinde CargoTrackUrl dolu olan siparişler)
            if (cargoTrackCount > 0)
            {
                badges.Add(new StatusCountBadge 
                { 
                    Status = OrderStatusType.OrderinCargo, // Geçici olarak aynı status kullanıyoruz, ama IsCargoTrackFilter ile ayırt edeceğiz
                    Label = "Kargodaki", 
                    Count = cargoTrackCount,
                    IconClass = "fas fa-shipping-fast",
                    IsCargoTrackFilter = true
                });
            }

            // Only show badges with count > 0
            return badges.Where(b => b.Count > 0).ToList();
        }
        
        private int GetTotalOrderCount()
        {
            return orders?.Count ?? 0;
        }

        private void ApplyFilterAndPagination()
        {
            // Apply "Kargodaki" filter (OrderItems içinde CargoTrackUrl dolu olan siparişler)
            if (isCargoFilterActive)
            {
                filteredOrders = orders?.Where(o => 
                    o.OrderItems != null && 
                    o.OrderItems.Any(item => !string.IsNullOrEmpty(item.CargoTrackUrl))
                ).ToList();
            }
            // Apply status filter
            else if (selectedStatusFilter.HasValue)
            {
                filteredOrders = orders?.Where(o => o.OrderStatusType == selectedStatusFilter.Value).ToList();
            }
            else
            {
                filteredOrders = orders?.ToList();
            }
            
            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchOrderNumber) && filteredOrders != null)
            {
                var searchTerm = searchOrderNumber.Trim().ToLowerInvariant();
                filteredOrders = filteredOrders
                    .Where(o => o.OrderNumber != null && o.OrderNumber.ToLowerInvariant().Contains(searchTerm))
                    .ToList();
            }
            
            // Calculate pagination
            if (filteredOrders != null && filteredOrders.Any())
            {
                totalPages = (int)Math.Ceiling(filteredOrders.Count / (double)pageSize);
                if (currentPage > totalPages)
                    currentPage = totalPages;
                if (currentPage < 1)
                    currentPage = 1;
            }
            else
            {
                totalPages = 1;
                currentPage = 1;
            }
            
            StateHasChanged();

            // Auto-expand if only one result found
            if (filteredOrders != null && filteredOrders.Count == 1)
            {
                var singleOrder = filteredOrders.First();
                if (!expandedOrderIds.Contains(singleOrder.Id))
                {
                    expandedOrderIds.Add(singleOrder.Id);
                }
            }
        }
        
        private List<OrderListDto> GetCurrentPageOrders()
        {
            if (filteredOrders == null || !filteredOrders.Any())
                return new List<OrderListDto>();
            
            return filteredOrders
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }
        
        private void OnStatusFilterChanged(OrderStatusType? value)
        {
            selectedStatusFilter = value;
            isCargoFilterActive = false; // "Kargodaki" filtresini kapat
            currentPage = 1; // Reset to first page when filter changes
            ApplyFilterAndPagination();
        }

        private void OnCargoTrackFilterChanged()
        {
            isCargoFilterActive = true;
            selectedStatusFilter = null; // Status filtresini kapat
            currentPage = 1; // Reset to first page when filter changes
            ApplyFilterAndPagination();
        }

        private void HandleSearchKeyPress(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
        {
            if (e.Key == "Enter" && !string.IsNullOrWhiteSpace(searchOrderNumber))
            {
                ApplySearch();
            }
        }

        private void ApplySearch()
        {
            currentPage = 1; // Reset to first page when searching
            ApplyFilterAndPagination();
        }

        private void ClearSearch()
        {
            searchOrderNumber = string.Empty;
            currentPage = 1;
            ApplyFilterAndPagination();
        }
        
        private void GoToPage(int page)
        {
            if (page >= 1 && page <= totalPages)
            {
                currentPage = page;
                StateHasChanged();
            }
        }
        
        private void OpenCargoTracking(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                NavigationManager.NavigateTo(url, forceLoad: true);
            }
        }

        private string GetProductImageUrl(OrderItems item)
        {
            if (item.ProductImages != null && item.ProductImages.Any())
            {
                var image = item.ProductImages.FirstOrDefault();
                if (image != null && !string.IsNullOrEmpty(image.Root))
                {
                    // If Root is already a full URL, return as-is
                    if (image.Root.StartsWith("http://") || image.Root.StartsWith("https://"))
                    {
                        return image.Root;
                    }
                    
                    // Otherwise, prepend CDN base URL
                    var cdnBaseUrl = Configuration["Cdn:BaseUrl"] ?? "https://cdn.yedeksen.com/images/";
                    var baseUrl = cdnBaseUrl.TrimEnd('/');
                    
                    // Remove leading slash from Root if present
                    var imagePath = image.Root.TrimStart('/');
                    
                    return $"{baseUrl}/ProductImages/{imagePath}";
                }
            }
            return "/images/no-photo.png";
        }

        // e-Fatura PDF indirme state
        private string? _downloadingEttn;

        /// <summary>
        /// e-Fatura PDF indirme — LinkedInvoices'tan çağrılır
        /// </summary>
        private async Task DownloadEInvoicePdf(ecommerce.Admin.Domain.Dtos.OrderDto.OrderLinkedInvoiceDto fatura)
        {
            if (_downloadingEttn != null || string.IsNullOrEmpty(fatura.Ettn)) return;

            _downloadingEttn = fatura.Ettn;
            StateHasChanged();

            try
            {
                var response = await OdaksoftInvoiceService.DownloadOutboxInvoiceAsync(fatura.Ettn);

                if (response.Status && !string.IsNullOrEmpty(response.ByteArray))
                {
                    var fileName = $"Fatura_{fatura.InvoiceNo}_{fatura.Ettn}.pdf";
                    await JSRuntime.InvokeVoidAsync("downloadFileFromBase64", new object?[] { response.ByteArray, fileName, "application/pdf" });

                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Başarılı",
                        Detail = $"{fatura.InvoiceNo} faturası indirildi."
                    });
                }
                else if (response.Status && !string.IsNullOrEmpty(response.Html))
                {
                    await JSRuntime.InvokeVoidAsync("openHtmlInNewTab", new object?[] { response.Html });
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, "Fatura indirilemedi", response.Message ?? "Bilinmeyen hata");
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "İndirme hatası", ex.Message);
            }
            finally
            {
                _downloadingEttn = null;
                StateHasChanged();
            }
        }

        public void Dispose()
        {
            // Cleanup if needed
        }

        [Inject] protected Microsoft.JSInterop.IJSRuntime JSRuntime { get; set; } = null!;

        // Dictionary to store ElementReferences by ID
        protected Dictionary<string, ElementReference> _elementReferences = new();

        protected void ShowTooltip(string elementId, string text)
        {
            if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(elementId) && _elementReferences.TryGetValue(elementId, out var elementRef))
            {
                TooltipService.Open(elementRef, text, new TooltipOptions 
                { 
                    Duration = 0,
                    Style = "background-color: #0e947a !important; background: #0e947a !important; color: white !important; border: none !important; padding: 8px 12px !important; border-radius: 4px !important; font-size: 0.875rem !important;"
                });
            }
        }

        protected void HideTooltip()
        {
            TooltipService.Close();
        }
    }
}
