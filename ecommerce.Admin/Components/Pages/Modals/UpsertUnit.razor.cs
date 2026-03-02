using ecommerce.Admin.Domain.Dtos.UnitDto;
using ecommerce.Admin.Services;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Helpers;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertUnit
    {
        #region Injection
        [Inject] protected DialogService DialogService { get; set; } = null!;
        [Inject] protected NotificationService NotificationService { get; set; } = null!;
        [Inject] protected AuthenticationService Security { get; set; } = null!;
        [Inject] public IUnitService UnitService { get; set; } = null!;
        #endregion

        [Parameter] public int? Id { get; set; }

        protected UnitUpsertDto? unit;
        protected bool Saving { get; set; }

        protected override async Task OnInitializedAsync()
        {
            if (Id.HasValue)
            {
                var response = await UnitService.GetUnitById(Id.Value);
                if (response.Ok && response.Result != null)
                {
                    unit = response.Result;
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                    unit = new UnitUpsertDto();
                }
            }
            else
            {
                unit = new UnitUpsertDto();
            }
        }

        protected async Task FormSubmit(UnitUpsertDto args)
        {
            try
            {
                Saving = true;

                var response = await UnitService.UpsertUnit(new AuditWrapDto<UnitUpsertDto>
                {
                    UserId = Security.User.Id,
                    Dto = args
                });

                if (response.Ok)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Başarılı",
                        Detail = Id.HasValue ? "Birim başarıyla güncellendi." : "Birim başarıyla eklendi."
                    });
                    DialogService.Close(args);
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, ex.Message);
            }
            finally
            {
                Saving = false;
            }
        }
    }
}
