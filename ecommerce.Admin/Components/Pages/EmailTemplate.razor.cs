using ecommerce.Admin.Components.Pages.Modals;
using ecommerce.Admin.Domain.Dtos.EmailDto;
using ecommerce.Admin.Domain.Dtos.StaticPageDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
namespace ecommerce.Admin.Components.Pages;
public partial class EmailTemplate
    {
        #region Injections

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
        protected AuthenticationService Security { get; set; }

        [Inject]
        public IEmailTemplateService EmailTemplateService { get; set; }

        #endregion

        int count;
        protected List<EmailTemplatesDto> emailTemplateList = null;
        protected RadzenDataGrid<EmailTemplatesDto>? radzenDataGrid = new();
        private PageSetting pager;
        private DialogOptions dialogOptions = new() { Width = "1200px" };

        protected async Task AddButtonClick(MouseEventArgs args)
        {
            await DialogService.OpenAsync<UpsertEmailTemplate>("Statik Sayfa Ekle/Düzenle", null, dialogOptions);
            await radzenDataGrid.Reload();
        }

        protected async Task EditRow(EmailTemplatesDto args)
        {
            await DialogService.OpenAsync<UpsertEmailTemplate>("Statik Sayfa Düzenle", new Dictionary<string, object> {
                { "Id", args.Id }
            }, dialogOptions);
            await radzenDataGrid.Reload();
        }

        protected async Task GridDeleteButtonClick(MouseEventArgs args, EmailTemplatesDto emailTemplatesDto)
        {
            if (await DialogService.Confirm("Seçilen kayıdı silmek istediğinize emin misiniz?", "Kayıt Sil", new ConfirmOptions()
            {
                OkButtonText = "Evet",
                CancelButtonText = "Hayır"
            }) == true)
            {
                var deleteResult = await EmailTemplateService.Delete(new Core.Helpers.AuditWrapDto<EmailTemplatesDto>()
                {
                    UserId = Security.User.Id,
                    Dto = new EmailTemplatesDto() { Id = emailTemplatesDto.Id }
                });

                if (deleteResult != null)
                {
                    if (deleteResult.Ok)
                        await radzenDataGrid.Reload();
                    else
                        await DialogService.Alert(deleteResult.Metadata.Message, "Uyarı", new AlertOptions() { OkButtonText = "Tamam" });
                }
            }
        }

        private async Task LoadData(LoadDataArgs args)
        {
            pager = new PageSetting(args.Filter, args.OrderBy, args.Skip, args.Top);

            var response = await EmailTemplateService.GetAllPaging(pager);
            if (response.Ok && response.Result != null)
            {
                emailTemplateList = response.Result.Data?.ToList();
                count = response.Result.DataCount;
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
            StateHasChanged();
        }
    }