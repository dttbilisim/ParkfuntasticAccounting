using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Blazored.Modal;
using Blazored.Modal.Services;
using ecommerce.Web.Components.Modals;
using ecommerce.Web.Domain.Dtos.Cart;
using ecommerce.Web.Utility;
using I18NPortable;
using Microsoft.AspNetCore.Components;

namespace ecommerce.Web.Components.Shared.Cart;

public partial class CartCargoSelector : ComponentBase
{
    [Inject] protected AppStateManager AppStateManager { get; set; } = null!;

    [Parameter] public CartSellerDto Seller { get; set; } = null!;
    [Parameter] public CartCargoDto? SelectedCargo { get; set; }
    [Parameter] public EventCallback<CartCargoDto> SelectedCargoChanged { get; set; }
    [Parameter] public bool IsDisabled { get; set; }
    [Parameter] public bool HideHeader { get; set; }
    [Parameter] public string? Class { get; set; }
    [CascadingParameter] protected II18N lang { get; set; } = null!;
    [CascadingParameter] public IModalService? ModalService { get; set; }

    private string GetCargoInputId(CartCargoDto cargo) => $"seller-{Seller.SellerId}-cargo-{cargo.CargoId}";
    private string GetCargoGroupName() => $"seller-{Seller.SellerId}-cargo";

    private bool HasCargoes => CargoList.Any();
    private IEnumerable<CartCargoDto> CargoList =>
        Seller?.Cargoes ?? Enumerable.Empty<CartCargoDto>();

    private bool ShowProgress => SelectedCargo?.SelectedProperty != null && SelectedCargo.Properties?.Any() == true;

    private int CargoPropertyPercent
    {
        get
        {
            if (SelectedCargo?.SelectedProperty == null || SelectedCargo.Properties == null || SelectedCargo.Properties.Count == 0)
            {
                return 0;
            }

            var index = SelectedCargo.Properties.FindIndex(p => p.Size == SelectedCargo.SelectedProperty.Size);
            if (index < 0)
            {
                index = 0;
            }

            var step = 100d / SelectedCargo.Properties.Count;
            var percent = (int)Math.Round((index + 1) * step);
            return Math.Clamp(percent, 0, 100);
        }
    }

    private bool IsCargoOverloaded => SelectedCargo?.SelectedProperty != null && Seller?.Desi > SelectedCargo.SelectedProperty.DesiMaxValue;

    private bool HasCampaign => SelectedCargo?.MinBasketAmount > 0;

    private decimal CampaignDifference
    {
        get
        {
            if (!HasCampaign)
            {
                return 0;
            }

            var difference = SelectedCargo!.MinBasketAmount - Seller.SubTotal;
            return difference > 0 ? difference : 0;
        }
    }

    private string CurrentSizeLabel
    {
        get
        {
            return SelectedCargo?.SelectedProperty?.Size ?? "—";
        }
    }

    private decimal RemainingAmount
    {
        get
        {
            if (SelectedCargo == null || SelectedCargo.MinBasketAmount <= 0)
            {
                return 0;
            }

            var remaining = SelectedCargo.MinBasketAmount - (Seller?.SubTotal ?? 0);
            return remaining > 0 ? remaining : 0;
        }
    }

    private async Task OnCargoChangedAsync(CartCargoDto cargo)
    {
        if (cargo == null || SelectedCargo?.CargoId == cargo.CargoId)
        {
            return;
        }

        SelectedCargo = cargo;

        await AppStateManager.ExecuteWithLoading(async () =>
        {
            var preferences = await AppStateManager.GetCartPreferences();
            preferences.SelectedCargoes[Seller.SellerId] = cargo.CargoId;
            await AppStateManager.SetCartPreferences(preferences);

            if (SelectedCargoChanged.HasDelegate)
            {
                await SelectedCargoChanged.InvokeAsync(cargo);
            }

            await AppStateManager.UpdatedCart(this, null);
        });
    }

    private async Task ShowCargoDetailAsync()
    {
        if (ModalService == null || SelectedCargo?.Properties == null || !SelectedCargo.Properties.Any())
        {
            return;
        }

        var parameters = new ModalParameters
        {
            { nameof(CargoDetailModal.SelectedCargo), SelectedCargo }
        };

        var options = new ModalOptions
        {
            DisableBackgroundCancel = true,
            HideHeader = true,
            HideCloseButton = true,
            Size = ModalSize.Large,
            AnimationType = ModalAnimationType.None
        };

        ModalService.Show<CargoDetailModal>(string.Empty, parameters, options);
        await Task.CompletedTask;
    }

    private string FormatPrice(CartCargoDto cargo)
    {
        if (cargo == null)
        {
            return L("Cart.FreeShipping");
        }

        return cargo.CargoPrice > 0 ? FormatCurrency(cargo.CargoPrice) : L("Cart.FreeShipping");
    }

    private string FormatCurrency(decimal value) => value.ToString("C", CultureInfo.CurrentCulture);

    private string L(string key)
    {
        try
        {
            var value = lang?[key];
            if (!string.IsNullOrWhiteSpace(value) && !string.Equals(value, key, StringComparison.Ordinal))
            {
                return value;
            }

            value = I18N.Current?.Translate(key);
            return string.IsNullOrWhiteSpace(value) ? key : value;
        }
        catch
        {
            return key;
        }
    }

    protected async Task OnSelectChanged(ChangeEventArgs e)
    {
        var str = e.Value?.ToString();
        if (string.IsNullOrWhiteSpace(str)) return;
        if (!int.TryParse(str, out var id)) return;
        var cargo = CargoList.FirstOrDefault(c => c.CargoId == id);
        if (cargo != null)
        {
            await OnCargoChangedAsync(cargo);
        }
    }

    private async Task OpenCargoDetailModal()
    {
        if (ModalService == null || SelectedCargo == null)
        {
            return;
        }

        var parameters = new ModalParameters()
            .Add(nameof(CargoDetailModal.SelectedCargo), SelectedCargo);

        var options = new ModalOptions
        {
            DisableBackgroundCancel = false,
            HideCloseButton = true
            
        };

        await ModalService.Show<CargoDetailModal>("", parameters, options).Result;
    }

}

