using AutoMapper;
using ecommerce.Admin.Domain.Dtos.NotificationTypeDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Admin.Services;
using ecommerce.Core.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using Microsoft.AspNetCore.Components.Web;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertNotificationType
    {
        #region Injection

        [Inject]
        protected IJSRuntime JSRuntime { get; set; }

        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected DialogService DialogService { get; set; }

        [Inject]
        protected TooltipService TooltipService { get; set; }

        [Inject]
        protected ContextMenuService ContextMenuService { get; set; }

        [Inject]
        protected NotificationService NotificationService { get; set; }
        [Inject]
        public INotificationTypeService Service { get; set; }

        [Inject]
        public IMapper Mapper { get; set; }

        [Inject]
        protected AuthenticationService Security { get; set; }

        #endregion

        [Parameter]
        public int? Id { get; set; }

        protected bool errorVisible;
        protected NotificationTypeUpsertDto notificationType = new();

        IEnumerable<NotificationTypeList> notificationTypeList = Enum.GetValues(typeof(NotificationTypeList)).Cast<NotificationTypeList>();


        protected override async Task OnInitializedAsync()
        {

            if (Id.HasValue)
            {
                var response = await Service.GetNotificationTypeById(Id.Value);
                if (response.Ok && response.Result != null)
                {
                    notificationType = response.Result;
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
                if (notificationType.NotificationTypeList == NotificationTypeList.None)
                {
                    NotificationService.Notify(NotificationSeverity.Error, "Lütfen bildirim tipi seçiniz");
                    return;
                }
                
                notificationType.Id = Id;

                var submitRs = await Service.UpsertNotificationType(new Core.Helpers.AuditWrapDto<NotificationTypeUpsertDto>()
                {
                    UserId = Security.User.Id,
                    Dto = notificationType
                });
                if (submitRs.Ok)
                {

                    DialogService.Close(notificationType);
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
