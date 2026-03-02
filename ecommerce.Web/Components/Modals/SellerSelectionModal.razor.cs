using Blazored.Modal;
using Blazored.Modal.Services;
using ecommerce.Domain.Shared.Dtos.Product;
using Microsoft.AspNetCore.Components;

namespace ecommerce.Web.Components.Modals;

public partial class SellerSelectionModal : ComponentBase
{
    [CascadingParameter] public BlazoredModalInstance ModalInstance { get; set; } = default!;
    
    [Parameter] public ProductElasticDto? SelectedProduct { get; set; }
    
    [Parameter] public List<SellerItemDto>? Sellers { get; set; }
    
    [Parameter] public EventCallback<SellerItemDto> OnSellerSelected { get; set; }

    private async Task SelectSeller(SellerItemDto seller)
    {
        if (seller.Stock <= 0) return;
        
        await OnSellerSelected.InvokeAsync(seller);
        await ModalInstance.CloseAsync();
    }

    private async Task CloseModal()
    {
        await ModalInstance.CloseAsync();
    }
}
