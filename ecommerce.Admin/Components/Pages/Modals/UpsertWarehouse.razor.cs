using AutoMapper;
using ecommerce.Admin.Domain.Dtos.WarehouseDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Admin.Domain.Dtos.HierarchicalDto;
using ecommerce.Admin.Domain.Dtos.MembershipDto;
using ecommerce.Core.Interfaces;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertWarehouse
    {
        #region Injections

        [Inject]
        protected DialogService DialogService { get; set; }

        [Inject]
        protected NotificationService NotificationService { get; set; }

        [Inject]
        public IWarehouseService Service { get; set; }

        [Inject]
        public ITenantProvider TenantProvider { get; set; }

        [Inject]
        public ICityService CityService { get; set; }

        [Inject]
        public ITownService TownService { get; set; }

        [Inject]
        protected AuthenticationService Security { get; set; }
        #endregion

        #region Parameters

        [Parameter]
        public int? Id { get; set; }

        #endregion

        private bool IsSaveButtonDisabled = false;
        protected bool errorVisible;
        protected WarehouseUpsertDto dto = new();
        public bool Status { get; set; } = true;

        protected List<CityListDto> cities = new();
        protected List<TownListDto> towns = new();

        protected override async Task OnInitializedAsync()
        {
            await LoadCities();

            // Multi-tenant: Şirket ve şube oturumdan alınır, kullanıcıya seçtirilmez
            var currentCorporationId = TenantProvider?.GetCurrentCorporationId() ?? 0;
            var currentBranchId = TenantProvider?.GetCurrentBranchId() ?? 0;
            dto.CorporationId = currentCorporationId;
            dto.BranchId = currentBranchId;

            if (Id.HasValue && Id.Value > 0)
            {
                var response = await Service.GetWarehouseById(Id.Value);
                if (response.Ok && response.Result != null)
                {
                    dto = response.Result;
                    Status = dto.Status == (int)EntityStatus.Active;
                    dto.CorporationId = currentCorporationId;
                    dto.BranchId = currentBranchId;

                    if (dto.CityId.HasValue)
                    {
                        await LoadTowns(dto.CityId.Value);
                    }

                    if (dto.Status == EntityStatus.Deleted.GetHashCode())
                        IsSaveButtonDisabled = true;
                }
                else
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
        }

        protected async Task LoadCities()
        {
            var response = await CityService.GetCities();
            if (response.Ok)
            {
                cities = response.Result;
            }
        }

        protected async Task OnCityChange(object value)
        {
            if (value is int cityId)
            {
                dto.TownId = null;
                await LoadTowns(cityId);
            }
            else
            {
                towns.Clear();
                dto.TownId = null;
            }
        }

        protected async Task LoadTowns(int cityId)
        {
            var response = await TownService.GetTownsByCityId(cityId);
            if (response.Ok)
            {
                towns = response.Result;
            }
        }

        protected async Task FormSubmit()
        {
            try
            {
                dto.Id = Id;
                dto.StatusBool = Status;
                var submitRs = await Service.UpsertWarehouse(new Core.Helpers.AuditWrapDto<WarehouseUpsertDto>()
                {
                    UserId = Security.User.Id,
                    Dto = dto
                });
                if (submitRs.Ok)
                {
                    NotificationService.Notify(NotificationSeverity.Success, "İşlem Başarılı");
                    DialogService.Close(dto);
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, submitRs.GetMetadataMessages());
                }
            }
            catch (Exception ex)
            {
                errorVisible = true;
                NotificationService.Notify(NotificationSeverity.Error, ex.ToString());
            }
        }

        protected void CancelButtonClick(MouseEventArgs args)
        {
            DialogService.Close(null);
        }
    }
}
