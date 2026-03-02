using ecommerce.Admin.Domain.Dtos.HierarchicalDto;
using ecommerce.Admin.Services;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertCorporation
    {
        [Parameter] public int? Id { get; set; }
        [Inject] public AuthenticationService AuthenticationService { get; set; } = null!;

        CorporationUpsertDto model = new CorporationUpsertDto();

        protected override async Task OnInitializedAsync()
        {
            if (Id.HasValue)
            {
                var result = await CorporationService.GetCorporationById(Id.Value);
                if (result.Ok)
                {
                    model = result.Result;
                }
            }
        }

        async Task HandleSubmit()
        {
            var user = AuthenticationService.User;
            var result = await CorporationService.UpsertCorporation(new AuditWrapDto<CorporationUpsertDto>
            {
                Dto = model,
                UserId = user.Id
            });

            if (result.Ok)
            {
                NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = "Başarılı", Detail = "Şirket kaydedildi" });
                DialogService.Close(true);
            }
            else
            {
                NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Hata", Detail = result.Metadata?.Message });
            }
        }
    }
}
