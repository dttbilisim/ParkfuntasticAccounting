using ecommerce.Admin.Domain.Dtos.OrderDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class OrderPendingModal
    {
        [Parameter] public int OrderId { get; set; }
        [Inject] private IOrderService OrderService { get; set; }
        [Inject] private NotificationService NotificationService { get; set; }
        [Inject] private AuthenticationService Security { get; set; }
        [Inject] private DialogService DialogService { get; set; }

        private OrderDetailModalDto? orderDetail;
        private bool isLoading = true;
        private bool isUpdating = false;
        private bool IsPlasiyer => Security?.User?.SalesPersonId != null;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                isLoading = true;
                var response = await OrderService.GetOrderDetailModal(OrderId);
                if (response.Ok && response.Result != null)
                    orderDetail = response.Result;
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                    DialogService.Close();
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Sipariş detayı yüklenirken hata oluştu.", ex.Message);
                DialogService.Close();
            }
            finally
            {
                isLoading = false;
            }
        }

        private void DecreaseQty(OrderItemUpdateDto item)
        {
            if (item.Quantity > 1) item.Quantity--;
        }

        private void IncreaseQty(OrderItemUpdateDto item)
        {
            item.Quantity++;
        }

        private void DecreasePkgQty(OrderPackageItemDto pkg)
        {
            if (pkg.Quantity > 0) pkg.Quantity--;
        }

        private void IncreasePkgQty(OrderPackageItemDto pkg)
        {
            pkg.Quantity++;
        }

        private void OnPkgQtyChanged(OrderPackageItemDto pkg, ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out var val) && val >= 0)
                pkg.Quantity = val;
        }

        private decimal GetItemTotal(OrderItemUpdateDto item)
        {
            if (item.IsPackageProduct && item.PackageProductItems != null && item.PackageProductItems.Any())
            {
                var subTotal = item.PackageProductItems.Sum(p => p.Price * p.Quantity);
                return subTotal * item.Quantity - (item.DiscountAmount ?? 0);
            }
            return item.Price * item.Quantity - (item.DiscountAmount ?? 0);
        }

        private async Task OnUpdateOnly()
        {
            if (orderDetail == null) return;
            isUpdating = true;
            try
            {
                foreach (var item in orderDetail.Items)
                {
                    if (item.IsPackageProduct && item.PackageProductItems != null && item.PackageProductItems.Any())
                    {
                        var subTotal = item.PackageProductItems.Sum(p => p.Price * p.Quantity);
                        item.TotalPrice = subTotal * item.Quantity - (item.DiscountAmount ?? 0);
                    }
                    else
                    {
                        item.TotalPrice = item.Price * item.Quantity - (item.DiscountAmount ?? 0);
                    }
                }
                var updateResult = await OrderService.UpdateOrderDetails(OrderId, orderDetail.CreatedDate, orderDetail.Voucher, orderDetail.GuideName, orderDetail.Items);
                if (updateResult.Ok)
                {
                    NotificationService.Notify(NotificationSeverity.Success, "Sipariş güncellendi.");
                    DialogService.Close(true); // Listeyi yenilemek için
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, updateResult.GetMetadataMessages());
                }
            }
            finally
            {
                isUpdating = false;
            }
        }

        private void Close()
        {
            DialogService.Close();
        }
    }
}
