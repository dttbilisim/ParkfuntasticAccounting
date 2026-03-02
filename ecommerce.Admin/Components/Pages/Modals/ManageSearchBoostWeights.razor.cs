using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Domain.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Newtonsoft.Json;
using Radzen;
using ecommerce.Core.Helpers;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Admin.Domain.Dtos.AppSettingDto;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class ManageSearchBoostWeights
    {
        [Inject]
        protected DialogService DialogService { get; set; }

        [Inject]
        protected NotificationService NotificationService { get; set; }

        [Inject]
        public IAppSettingService AppSettingService { get; set; }

        [Inject]
        protected AuthenticationService Security { get; set; }

        [Parameter]
        public int Id { get; set; }

        protected bool errorVisible;
        protected string errorMessage = "Ayar kaydedilemedi!";
        protected SearchBoostSettings settings = new();
        protected string settingDescription = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            if (Id > 0)
            {
                var response = await AppSettingService.GetSettingById(Id);
                if (response.Ok && response.Result != null)
                {
                    settingDescription = response.Result.Description;
                    try
                    {
                        if (!string.IsNullOrEmpty(response.Result.Value))
                        {
                            settings = JsonConvert.DeserializeObject<SearchBoostSettings>(response.Result.Value) ?? new();
                        }
                    }
                    catch (Exception ex)
                    {
                        NotificationService.Notify(NotificationSeverity.Error, "JSON ayrıştırma hatası: " + ex.Message);
                    }
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
                var jsonValue = JsonConvert.SerializeObject(settings, Formatting.Indented);
                
                var upsertDto = new AppSettingUpsertDto
                {
                    Id = Id,
                    Key = "Search_BoostWeights",
                    Value = jsonValue,
                    Description = settingDescription
                };

                var auditDto = new AuditWrapDto<AppSettingUpsertDto>
                {
                    UserId = Security.User.Id,
                    Dto = upsertDto
                };

                var response = await AppSettingService.UpsertSetting(auditDto);
                if (response.Ok)
                {
                    NotificationService.Notify(NotificationSeverity.Success, "Arama ağırlıkları başarıyla güncellendi");
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
