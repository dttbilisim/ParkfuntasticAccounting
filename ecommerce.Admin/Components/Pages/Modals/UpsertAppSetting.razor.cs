using ecommerce.Admin.Domain.Dtos.AppSettingDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ecommerce.Core.Utils.ResultSet;
using Radzen;
using ecommerce.Core.Helpers;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertAppSetting
    {
        [Inject]
        protected DialogService DialogService { get; set; }

        [Inject]
        protected NotificationService NotificationService { get; set; }

        [Inject]
        public IAppSettingService Service { get; set; }

        [Inject]
        protected AuthenticationService Security { get; set; }

        [Parameter]
        public int? Id { get; set; }

        protected bool errorVisible;
        protected string errorMessage = "Ayar kaydedilemedi!";
        protected AppSettingUpsertDto dto = new();

        protected override async Task OnInitializedAsync()
        {
            if (Id.HasValue && Id.Value > 0)
            {
                var response = await Service.GetSettingById(Id.Value);
                if (response.Ok && response.Result != null)
                {
                    dto = response.Result;
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                }
            }
        }

        protected async Task FormSubmit()
        {
            try
            {
                var auditDto = new AuditWrapDto<AppSettingUpsertDto>
                {
                    UserId = Security.User.Id,
                    Dto = dto
                };

                var response = await Service.UpsertSetting(auditDto);
                if (response.Ok)
                {
                    NotificationService.Notify(NotificationSeverity.Success, "Başarıyla kaydedildi");
                    DialogService.Close(true);
                }
                else
                {
                    errorVisible = true;
                    errorMessage = response.GetMetadataMessages();
                    NotificationService.Notify(NotificationSeverity.Error, errorMessage);
                }
            }
            catch (Exception ex)
            {
                errorVisible = true;
                errorMessage = ex.Message;
                NotificationService.Notify(NotificationSeverity.Error, ex.Message);
            }
        }

        protected void CancelButtonClick(MouseEventArgs args)
        {
            DialogService.Close(null);
        }
    }
}
