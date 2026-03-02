using ecommerce.Admin.Domain.Dtos.HierarchicalDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertBranch
    {
        [Parameter] public int? Id { get; set; }
        [Inject] public AuthenticationService AuthenticationService { get; set; } = null!;
        [Inject] public ICityService CityService { get; set; } = null!;
        [Inject] public ITownService TownService { get; set; } = null!;

        BranchUpsertDto model = new BranchUpsertDto();
        IEnumerable<CorporationListDto> corporations = new List<CorporationListDto>();
        IEnumerable<ecommerce.Admin.Domain.Dtos.MembershipDto.CityListDto> cities = new List<ecommerce.Admin.Domain.Dtos.MembershipDto.CityListDto>();
        IEnumerable<ecommerce.Admin.Domain.Dtos.MembershipDto.TownListDto> towns = new List<ecommerce.Admin.Domain.Dtos.MembershipDto.TownListDto>();

        protected override async Task OnInitializedAsync()
        {
            var corpResult = await CorporationService.GetAllActiveCorporations();
            if (corpResult.Ok)
            {
                corporations = corpResult.Result;
            }

            var cityResult = await CityService.GetCities();
            if (cityResult.Ok)
            {
                cities = cityResult.Result;
            }

            if (Id.HasValue)
            {
                var result = await BranchService.GetBranchById(Id.Value);
                if (result.Ok)
                {
                    model = result.Result;
                    if (model.CityId.HasValue)
                    {
                        await LoadTowns(model.CityId.Value);
                    }
                }
            }
        }

        async Task OnCityChange(object value)
        {
            if (value is int cityId)
            {
                model.TownId = null;
                await LoadTowns(cityId);
            }
        }

        async Task LoadTowns(int cityId)
        {
            var townResult = await TownService.GetTownsByCityId(cityId);
            if (townResult.Ok)
            {
                towns = townResult.Result;
            }
        }

        async Task HandleSubmit()
        {
            var user = AuthenticationService.User;
            var result = await BranchService.UpsertBranch(new AuditWrapDto<BranchUpsertDto>
            {
                Dto = model,
                UserId = user.Id
            });

            if (result.Ok)
            {
                NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = "Başarılı", Detail = "Şube kaydedildi" });
                DialogService.Close(true);
            }
            else
            {
                NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Hata", Detail = result.Metadata?.Message });
            }
        }
    }
}
