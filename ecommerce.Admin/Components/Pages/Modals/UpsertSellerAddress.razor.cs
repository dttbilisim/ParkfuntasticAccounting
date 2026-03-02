using ecommerce.Admin.Domain.Dtos.SellerAddressDto;
using ecommerce.Admin.Domain.Dtos.MembershipDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Helpers;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertSellerAddress
    {
        [Inject] protected IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
        [Inject] protected DialogService DialogService { get; set; } = default!;
        [Inject] protected NotificationService NotificationService { get; set; } = default!;
        [Inject] public ISellerAddressService SellerAddressService { get; set; } = default!;
        [Inject] public ICityService CityService { get; set; } = default!;
        [Inject] public ITownService TownService { get; set; } = default!;
        [Inject] protected AuthenticationService Security { get; set; } = default!;

        [Parameter] public int? Id { get; set; }
        [Parameter] public int SellerId { get; set; }

        protected bool errorVisible;
        protected SellerAddressUpsertDto model = new();
        protected List<CityListDto> cities = new();
        protected List<TownListDto> towns = new();

        protected override async Task OnInitializedAsync()
        {
            await LoadCitiesAsync();

            if (Id.HasValue)
            {
                var response = await SellerAddressService.GetSellerAddressById(Id.Value);
                if (response.Ok && response.Result != null)
                {
                    model = response.Result;
                    if (model.CityId > 0)
                    {
                        await LoadTownsAsync(model.CityId);
                    }
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                }
            }
        }

        private async Task LoadCitiesAsync()
        {
            var response = await CityService.GetCities();
            if (response.Ok && response.Result != null)
            {
                cities = response.Result;
            }
        }

        private async Task LoadTownsAsync(int cityId)
        {
            var response = await TownService.GetTownsByCityId(cityId);
            if (response.Ok && response.Result != null)
            {
                towns = response.Result;
            }
            else
            {
                towns = new List<TownListDto>();
            }
        }

        protected async Task OnCityChanged(object value)
        {
            int cityId = value is int converted ? converted : 0;
            model.CityId = cityId;
            model.TownId = 0;
            towns.Clear();

            if (cityId > 0)
            {
                await LoadTownsAsync(cityId);
            }
        }

        protected async Task FormSubmit()
        {
            model.Id = Id;
            model.SellerId = SellerId;

            var submitRs = await SellerAddressService.UpsertSellerAddress(new AuditWrapDto<SellerAddressUpsertDto>
            {
                UserId = Security.User.Id,
                Dto = model
            });

            if (submitRs.Ok)
            {
                NotificationService.Notify(NotificationSeverity.Success, submitRs.GetMetadataMessages());
                DialogService.Close(null);
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, submitRs.GetMetadataMessages());
            }
        }

        protected void CancelButtonClick(MouseEventArgs args)
        {
            DialogService.Close(null);
        }
    }
}
