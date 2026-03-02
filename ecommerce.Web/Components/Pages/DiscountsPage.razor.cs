using ecommerce.Web.Domain.Dtos;
using ecommerce.Web.Domain.Services.Abstract;
using ecommerce.Web.Utility;
using I18NPortable;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ecommerce.Web.Components.Pages;

public partial class DiscountsPage
{
    [Inject] private IDiscountService _discountService { get; set; } = null!;
    [Inject] private AppStateManager _appStateManager { get; set; } = null!;
    [Inject] private II18N lang { get; set; } = null!;
    [Inject] private IJSRuntime _jsRuntime { get; set; } = null!;

    private List<DiscountDto> discounts = new();
    private bool isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadDiscounts();
    }

    private async Task LoadDiscounts()
    {
        isLoading = true;
        try
        {
            var result = await _discountService.GetActiveDiscountsAsync();
            if (result.Ok && result.Result != null)
            {
                discounts = result.Result;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading discounts: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task CopyCouponCode(string? couponCode)
    {
        if (string.IsNullOrEmpty(couponCode)) return;

        try
        {
            await _jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", couponCode);
            // TODO: Show success toast/notification
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error copying coupon code: {ex.Message}");
        }
    }
}
