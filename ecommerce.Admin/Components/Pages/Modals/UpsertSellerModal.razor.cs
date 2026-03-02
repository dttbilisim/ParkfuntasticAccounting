using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ecommerce.Admin.Domain.Dtos.CompanyCargoDto;
using ecommerce.Admin.Domain.Dtos.MembershipDto;
using ecommerce.Admin.Domain.Dtos.SellerAddressDto;
using ecommerce.Admin.Domain.Dtos.SellerDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Helpers;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using Radzen;
using Radzen.Blazor;
using ecommerce.Admin.Components.Layout;
using ecommerce.Admin.Resources;
using ecommerce.Admin.CustomComponents.Modals;
using FluentValidation;
using Microsoft.AspNetCore.Components;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertSellerModal
    {
        [Inject] public ISellerService SellerService { get; set; } = default!;
        [Inject] public ICityService CityService { get; set; } = default!;
        [Inject] public ITownService TownService { get; set; } = default!;
        [Inject] public ICompanyCargoService CompanyCargoService { get; set; } = default!;
        [Inject] public ISellerAddressService SellerAddressService { get; set; } = default!;
        [Inject] public AuthenticationService Security { get; set; } = default!;
        [Inject] protected DialogService DialogService { get; set; } = default!;
        [Inject] protected NotificationService NotificationService { get; set; } = default!;
        [Inject] private IValidator<SellerUpsertDto> SellerValidator { get; set; } = default!;
        [Inject] private IStringLocalizer<Culture_TR> RadzenLocalizer { get; set; } = default!;

        [Parameter] public int? Id { get; set; }

        protected SellerUpsertDto model = new();
        protected List<CityListDto> cities = new();
        protected List<TownListDto> towns = new();
        protected List<CompanyCargoListDto> companyCargos = new();
        protected List<SellerAddressListDto> sellerAddresses = new();
        private RadzenFluentValidator<SellerUpsertDto> SellerFluentValidator { get; set; } = default!;
        private bool Saving { get; set; }
        protected int selectedTabIndex;

        protected bool CanManageCargo => CurrentSellerId.HasValue;
        private int? CurrentSellerId => model.Id ?? Id;

        // ... (existing methods)

        protected async Task Save(SellerUpsertDto args)
        {
            Saving = true;
            var res = await SellerService.UpsertSeller(new AuditWrapDto<SellerUpsertDto>
            {
                UserId = Security.User.Id,
                Dto = args
            });
            if (res.Ok)
            {
                NotificationService.Notify(NotificationSeverity.Success, "Kayıt başarılı");
                if (!args.Id.HasValue && res.Result > 0)
                {
                    model.Id = res.Result;
                    Id = res.Result;
                    args.Id = res.Result;
                    
                    // Auto-switch to Cargo tab for new records
                    selectedTabIndex = 1;
                    StateHasChanged();
                }
                await LoadCompanyCargosAsync();
                await LoadSellerAddressesAsync();
                StateHasChanged();
            }
            else if (res.Exception != null)
            {
                NotificationService.Notify(NotificationSeverity.Error, res.Exception.Message);
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, res.GetMetadataMessages());
            }
            Saving = false;
        }

        protected override async Task OnInitializedAsync()
        {
            await LoadCitiesAsync();
            if (Id.HasValue)
            {
                var res = await SellerService.GetSellerById(Id.Value);
                if (res.Ok && res.Result != null)
                {
                    model = res.Result;
                    if (model.CityId.HasValue)
                    {
                        await LoadTownsAsync(model.CityId.Value);
                    }
                    await LoadCompanyCargosAsync();
                    await LoadSellerAddressesAsync();
                }
                else if (res.Exception != null)
                {
                    NotificationService.Notify(NotificationSeverity.Error, res.Exception.Message);
                }
            }
            else
            {
                model.Status = 1;
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
            int? cityId = value as int?;
            if (!cityId.HasValue && value is int converted)
            {
                cityId = converted;
            }

            model.CityId = cityId;
            model.TownId = null;
            towns.Clear();

            if (cityId.HasValue)
            {
                await LoadTownsAsync(cityId.Value);
            }
        }


        private async Task ShowErrors()
        {
            await DialogService.OpenAsync<ValidationModal>("Uyarı", new Dictionary<string, object>
            {
                { "Errors", SellerFluentValidator.GetValidationMessages().Select(p => new Dictionary<string, string> { { p.Key, p.Value } }).ToList() }
            });
        }

        private async Task LoadCompanyCargosAsync()
        {
            var sellerId = CurrentSellerId;
            if (!sellerId.HasValue)
            {
                companyCargos = new List<CompanyCargoListDto>();
                return;
            }

            var response = await CompanyCargoService.GetCompanyCargoes(sellerId.Value);
            if (response.Ok && response.Result != null)
            {
                companyCargos = response.Result.OrderByDescending(x => x.Id).ToList();
            }
            else if (response.Exception != null)
            {
                NotificationService.Notify(NotificationSeverity.Error, response.Exception.Message);
            }
        }

        protected async Task AddCargoAsync()
        {
            var sellerId = CurrentSellerId;
            if (!sellerId.HasValue)
            {
                return;
            }

            await DialogService.OpenAsync<UpsertCompanyCargo>("Kargo Ekle",
                new Dictionary<string, object>
                {
                    { "SellerId", sellerId.Value }
                },
                new DialogOptions { Width = "600px" });

            await LoadCompanyCargosAsync();
        }

        protected async Task EditCargoAsync(CompanyCargoListDto cargo)
        {
            var sellerId = CurrentSellerId;
            if (!sellerId.HasValue)
            {
                return;
            }

            await DialogService.OpenAsync<UpsertCompanyCargo>("Kargo Düzenle",
                new Dictionary<string, object>
                {
                    { "Id", cargo.Id },
                    { "SellerId", sellerId.Value }
                },
                new DialogOptions { Width = "600px" });

            await LoadCompanyCargosAsync();
        }

        protected async Task DeleteCargoAsync(CompanyCargoListDto cargo)
        {
            var confirm = await DialogService.Confirm("Seçilen kargo kaydı silinecek. Onaylıyor musunuz?", "Kargo Sil",
                new ConfirmOptions { OkButtonText = "Evet", CancelButtonText = "Hayır" });
            if (confirm != true)
            {
                return;
            }

            var response = await CompanyCargoService.DeleteCompanyCargo(new AuditWrapDto<CompanyCargoDeleteDto>
            {
                UserId = Security.User.Id,
                Dto = new CompanyCargoDeleteDto { Id = cargo.Id }
            });

            if (response.Ok)
            {
                NotificationService.Notify(NotificationSeverity.Success, response.GetMetadataMessages());
                await LoadCompanyCargosAsync();
            }
            else if (response.Exception != null)
            {
                NotificationService.Notify(NotificationSeverity.Error, response.Exception.Message);
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
        }

        private async Task LoadSellerAddressesAsync()
        {
            var sellerId = CurrentSellerId;
            if (!sellerId.HasValue)
            {
                sellerAddresses = new List<SellerAddressListDto>();
                return;
            }

            var response = await SellerAddressService.GetSellerAddresses(sellerId.Value);
            if (response.Ok && response.Result != null)
            {
                sellerAddresses = response.Result;
            }
            else if (response.Exception != null)
            {
                NotificationService.Notify(NotificationSeverity.Error, response.Exception.Message);
            }
        }

        protected async Task AddAddressAsync()
        {
            var sellerId = CurrentSellerId;
            if (!sellerId.HasValue)
            {
                return;
            }

            await DialogService.OpenAsync<UpsertSellerAddress>("Adres Ekle",
                new Dictionary<string, object>
                {
                    { "SellerId", sellerId.Value }
                },
                new DialogOptions { Width = "700px" });

            await LoadSellerAddressesAsync();
        }

        protected async Task EditAddressAsync(SellerAddressListDto address)
        {
            var sellerId = CurrentSellerId;
            if (!sellerId.HasValue)
            {
                return;
            }

            await DialogService.OpenAsync<UpsertSellerAddress>("Adres Düzenle",
                new Dictionary<string, object>
                {
                    { "Id", address.Id },
                    { "SellerId", sellerId.Value }
                },
                new DialogOptions { Width = "700px" });

            await LoadSellerAddressesAsync();
        }

        protected async Task DeleteAddressAsync(SellerAddressListDto address)
        {
            var confirm = await DialogService.Confirm("Seçilen adres kaydı silinecek. Onaylıyor musunuz?", "Adres Sil",
                new ConfirmOptions { OkButtonText = "Evet", CancelButtonText = "Hayır" });
            if (confirm != true)
            {
                return;
            }

            var response = await SellerAddressService.DeleteSellerAddress(new AuditWrapDto<SellerAddressDeleteDto>
            {
                UserId = Security.User.Id,
                Dto = new SellerAddressDeleteDto { Id = address.Id }
            });

            if (response.Ok)
            {
                NotificationService.Notify(NotificationSeverity.Success, response.GetMetadataMessages());
                await LoadSellerAddressesAsync();
            }
            else if (response.Exception != null)
            {
                NotificationService.Notify(NotificationSeverity.Error, response.Exception.Message);
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
        }

        protected Task Close()
        {
            DialogService.Close(null);
            return Task.CompletedTask;
        }
    }
}

