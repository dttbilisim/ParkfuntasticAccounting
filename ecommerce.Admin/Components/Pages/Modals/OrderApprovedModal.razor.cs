using ecommerce.Admin.Domain.Dtos.OrderDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class OrderApprovedModal
    {
        [Parameter] public int OrderId { get; set; }
        [Inject] private IOrderService OrderService { get; set; }
        [Inject] private NotificationService NotificationService { get; set; }
        [Inject] private DialogService DialogService { get; set; }

        private OrderDetailModalDto? orderDetail;
        private bool isLoading = true;

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

        private void Close()
        {
            DialogService.Close();
        }
    }
}
