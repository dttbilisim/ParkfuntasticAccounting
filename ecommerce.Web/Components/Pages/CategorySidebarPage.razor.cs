using ecommerce.Domain.Shared.Dtos.Product;
using ecommerce.Web.Domain.Services.Abstract;
using ecommerce.Web.Utility;
using ecommerce.Web.Domain.Dtos.Cart;
using ecommerce.Web.Events;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ecommerce.Web.Components.Pages;

public partial class CategorySidebarPage : IDisposable
{
    private CartDto CartResult = new();
    private CancellationTokenSource? _renderCts;
    private bool _isDisposed = false;
    [Parameter]
    [SupplyParameterFromQuery]
    public string? query { get; set; }
    [Inject] ISellerProductService _productService { get; set; }
    [Inject] private AppStateManager _appStateManager { get; set; }
    private List<SellerProductViewModel> searchResults = new();
    protected override async Task OnInitializedAsync()
    {
        _appStateManager.StateChanged += AppState_StateChanged;
        CartResult = await _appStateManager.GetCart();
    }

    private async void AppState_StateChanged(ComponentBase source, string property, CartDto? updatedCart)
    {
        if (_isDisposed || property != AppStateEvents.updateCart)
        {
            return;
        }

        try
        {
            CartResult = updatedCart ?? await _appStateManager.GetCart();
            await RequestRender();
        }
        catch (Exception ex)
        {
            if (!_isDisposed)
                Console.WriteLine($"⚠️ CategorySidebarPage.AppState_StateChanged error: {ex.Message}");
        }
    }

    private async Task RequestRender()
    {
        if (_isDisposed) return;

        var newCts = new CancellationTokenSource();
        var oldCts = Interlocked.Exchange(ref _renderCts, newCts);
        oldCts?.Cancel();
        oldCts?.Dispose();

        try
        {
            var token = newCts.Token;
            await Task.Delay(15, token); 
            
                await InvokeAsync(async () => {
                    if (!token.IsCancellationRequested && !_isDisposed)
                    {
                         base.StateHasChanged();
                    }
                });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            if (!_isDisposed)
                Console.WriteLine($"⚠️ RequestRender error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _isDisposed = true;
        _renderCts?.Cancel();
        _renderCts?.Dispose();
        _appStateManager.StateChanged -= AppState_StateChanged;
    }
    protected override async Task OnParametersSetAsync()
    {
        if (!string.IsNullOrWhiteSpace(query))
        {
            await LoadSearchProducts(query);
        }
    }

    private async Task LoadSearchProducts(string searchTerm)
    {
        var result = await _productService.SearchAsync(searchTerm); 
        
        if (result.Ok && result.Result is not null)
        {
            searchResults = result.Result;
        }
    }
    
}