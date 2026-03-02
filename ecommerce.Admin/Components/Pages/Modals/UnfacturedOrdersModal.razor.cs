using ecommerce.Admin.Domain.ViewModels;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Core.Utils;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UnfacturedOrdersModal
    {
        #region Injection
        [Inject] protected DialogService DialogService { get; set; } = null!;
        [Inject] protected NotificationService NotificationService { get; set; } = null!;
        [Inject] public IOrderService OrderService { get; set; } = null!;
        #endregion

        protected IEnumerable<UnfacturedOrderViewModel>? orders;
        protected IList<UnfacturedOrderViewModel>? selectedOrders;
        protected RadzenDataGrid<UnfacturedOrderViewModel>? dataGrid;
        protected decimal SelectedTotal => selectedOrders?.Sum(x => x.GrandTotal) ?? 0;
        
        // Accordion — açık olan sipariş ID'leri
        protected HashSet<int> _expandedOrderIds = new();

        // Satır genişletildiğinde tetiklenir
        protected void OnRowExpand(UnfacturedOrderViewModel order)
        {
            // Radzen ExpandMode.Single zaten sadece bir satır açık tutar
        }

        protected override async Task OnInitializedAsync()
        {
            await LoadOrders();
        }

        protected async Task LoadOrders()
        {
            try
            {
                var response = await OrderService.GetUnfacturedOrders();
                if (response.Ok && response.Result != null)
                {
                    orders = response.Result.Select(x => new UnfacturedOrderViewModel
                    {
                        Id = x.Id,
                        OrderNumber = x.OrderNumber,
                        CreatedDate = x.CreatedDate,
                        CustomerId = x.CustomerId,
                        CustomerName = x.CustomerName, // B2B Cari Name
                        BuyerName = x.BuyerName,       // Alıcı Address Name
                        GrandTotal = x.GrandTotal,
                        OrderStatusType = x.OrderStatusType,
                        ItemCount = x.OrderItems?.Count ?? 0,
                        // Sipariş ürün detayları — accordion'da gösterilecek
                        Items = x.OrderItems?.Select(oi => new UnfacturedOrderItemViewModel
                        {
                            ProductName = oi.ProductName ?? "Ürün",
                            Quantity = oi.Quantity,
                            Price = oi.Price,
                            TotalPrice = oi.TotalPrice,
                            DiscountAmount = oi.DiscountAmount
                        }).ToList() ?? new()
                    }).ToList();
                }
                else
                {
                    var errorMessage = !response.Ok ? "Faturalanmamış siparişler yüklenirken hata oluştu." : "Faturalanmamış sipariş bulunamadı.";
                    NotificationService.Notify(NotificationSeverity.Error, errorMessage);
                    orders = new List<UnfacturedOrderViewModel>();
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, $"Hata: {ex.Message}");
                orders = new List<UnfacturedOrderViewModel>();
            }
            
            await InvokeAsync(StateHasChanged);
            if (dataGrid != null)
            {
                await dataGrid.Reload();
            }
        }

        // Row seçildiğinde çalışır (Grid satırına tıklanınca)
        protected async Task OnRowSelect(UnfacturedOrderViewModel data)
        {
            // Listeyi kontrol et
            if (selectedOrders != null && selectedOrders.Count > 1)
            {
                // Mevcut listedeki diğer siparişleri (yeni eklenen hariç) kontrol et
                var existingOrders = selectedOrders.Where(x => x.Id != data.Id).ToList();
                
                if (existingOrders.Any())
                {
                    var validCustomerId = existingOrders.First().CustomerId;
                    
                    if (data.CustomerId != validCustomerId)
                    {
                        // Yeni eklenen sipariş farklı müşteriye ait, onu listeden çıkar
                        selectedOrders.Remove(data);
                        
                        NotificationService.Notify(NotificationSeverity.Warning, "Dikkat", "Sadece aynı müşteriye ait siparişleri seçebilirsiniz.");
                        
                        // Grid arayüzünü güncelle
                        if (dataGrid != null) await dataGrid.Reload();
                    }
                }
            }
            StateHasChanged();
        }

        // Seçim değiştiğinde tetiklenir (Tek satır Checkbox)
        protected async Task OnRowCheckboxChange(UnfacturedOrderViewModel data, bool? value)
        {
            if (selectedOrders == null) selectedOrders = new List<UnfacturedOrderViewModel>();

            if (value == true)
            {
                if (selectedOrders.Any())
                {
                    var validCustomerId = selectedOrders.First().CustomerId;
                    if (data.CustomerId != validCustomerId)
                    {
                         NotificationService.Notify(NotificationSeverity.Warning, "Dikkat", "Sadece aynı müşteriye ait siparişleri seçebilirsiniz.");
                         
                         // Checkbox UI'ını resetlemek için grid reload
                         if (dataGrid != null) await dataGrid.Reload();
                         
                         // UI'ın refresh olması için StateHasChanged çağırıp çıkıyoruz. 
                         // return DEMİYORUZ veya return öncesi StateHasChanged yapıyoruz.
                         // Aslında return diyebiliriz ama StateHasChanged şart.
                         await InvokeAsync(StateHasChanged);
                         return;
                    }
                }
                
                if (!selectedOrders.Contains(data))
                {
                    selectedOrders.Add(data);
                }
            }
            else
            {
                if (selectedOrders.Contains(data))
                {
                    selectedOrders.Remove(data);
                }
            }
            
            StateHasChanged();
        }

        protected void TransferSelectedOrders()
        {
            if (selectedOrders == null || !selectedOrders.Any())
            {
                NotificationService.Notify(NotificationSeverity.Warning, "Lütfen en az bir sipariş seçiniz.");
                return;
            }

            // Fail-Safe: Otomatik filtreleme
            var firstItem = selectedOrders.First();
            var validCustomerId = firstItem.CustomerId;
            
            if (selectedOrders.Any(x => x.CustomerId != validCustomerId))
            {
                 // Farklı müşteriye ait olanları tespit et ve çıkar
                 var validList = selectedOrders.Where(x => x.CustomerId == validCustomerId).ToList();
                 selectedOrders = validList;
                 
                 NotificationService.Notify(NotificationSeverity.Warning, "Otomatik Düzeltme", "Farklı müşterilere ait siparişler listeden çıkarıldı, sadece ilk seçilen müşteriye ait olanlar işleniyor.");
            }

            var selectedOrderIds = selectedOrders.Select(o => o.Id).Distinct().ToList();
            DialogService.Close(selectedOrderIds);
        }

        protected BadgeStyle GetStatusBadgeStyle(OrderStatusType status)
        {
            return status switch
            {
                OrderStatusType.OrderNew => BadgeStyle.Info,
                OrderStatusType.OrderWaitingApproval => BadgeStyle.Warning,
                OrderStatusType.OrderSuccess => BadgeStyle.Success,
                OrderStatusType.OrderCanceled => BadgeStyle.Danger,
                _ => BadgeStyle.Light
            };
        }

        protected string GetStatusText(OrderStatusType status)
        {
            return status switch
            {
                OrderStatusType.OrderNew => "Yeni",
                OrderStatusType.OrderWaitingApproval => "Onay Bekliyor",
                OrderStatusType.OrderSuccess => "Tamamlandı",
                OrderStatusType.OrderCanceled => "İptal",
                _ => status.ToString()
            };
        }
    }
}
