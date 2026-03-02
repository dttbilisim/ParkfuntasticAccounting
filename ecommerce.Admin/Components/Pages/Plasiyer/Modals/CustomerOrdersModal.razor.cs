using ecommerce.Admin.Domain.Dtos.OrderDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Core.Utils;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Plasiyer.Modals;

public partial class CustomerOrdersModal : ComponentBase
{
    [Inject] protected IOrderService OrderService { get; set; } = default!;
    [Inject] protected NotificationService NotificationService { get; set; } = default!;
    
    [Parameter] public int CustomerId { get; set; }
    
    protected List<OrderListDto>? Orders { get; set; }
    protected bool IsLoading { get; set; } = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadOrders();
    }

    private async Task LoadOrders()
    {
        IsLoading = true;
        try
        {
            var result = await OrderService.GetCustomerOrders(CustomerId);
            if (result.Ok)
            {
                Orders = result.Result;
            }
            else
            {
                NotificationService.Notify(new NotificationMessage 
                { 
                    Severity = NotificationSeverity.Error, 
                    Summary = "Hata", 
                    Detail = "Sipariş verileri alınırken bir hata oluştu.",
                    Duration = 4000 
                });
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage 
            { 
                Severity = NotificationSeverity.Error, 
                Summary = "Sistem Hatası", 
                Detail = "Beklenmedik bir hata oluştu: " + ex.Message,
                Duration = 5000 
            });
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected BadgeStyle GetStatusStyle(OrderStatusType status)
    {
        return status switch
        {
            OrderStatusType.OrderNew => BadgeStyle.Info,
            OrderStatusType.OrderPrepare => BadgeStyle.Warning,
            OrderStatusType.OrderinCargo => BadgeStyle.Primary,
            OrderStatusType.OrderSuccess => BadgeStyle.Success,
            OrderStatusType.PaymentSuccess => BadgeStyle.Success,
            OrderStatusType.OrderCanceled => BadgeStyle.Danger,
            _ => BadgeStyle.Secondary
        };
    }

    protected string GetEnumDescription(Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = (System.ComponentModel.DataAnnotations.DisplayAttribute?)Attribute.GetCustomAttribute(field, typeof(System.ComponentModel.DataAnnotations.DisplayAttribute));
        return attribute?.Description ?? value.ToString();
    }
}
