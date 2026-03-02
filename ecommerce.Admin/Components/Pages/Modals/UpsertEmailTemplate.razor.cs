using ecommerce.Admin.Domain.Dtos.EmailDto;
using ecommerce.Admin.Domain.Dtos.StaticPageDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Helpers.Concretes;
using ecommerce.Admin.Services;
using ecommerce.Core.Helpers;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;
namespace ecommerce.Admin.Components.Pages.Modals;
 public partial class UpsertEmailTemplate{
        #region Injections
        [Inject] protected IJSRuntime JSRuntime{get;set;}
        [Inject] protected NavigationManager NavigationManager{get;set;}
        [Inject] protected DialogService DialogService{get;set;}
        [Inject] protected TooltipService TooltipService{get;set;}
        [Inject] protected ContextMenuService ContextMenuService{get;set;}
        [Inject] protected NotificationService NotificationService{get;set;}
        [Inject] public IEmailTemplateService EmailTemplateService{get;set;}
        [Inject] public IAppSettingService AppSettingService{get;set;}
        [Inject] public IConfiguration Configuration{get;set;}
        [Inject] public IWebHostEnvironment Environment{get;set;}
        [Inject] protected AuthenticationService Security{get;set;}
        #endregion
        #region Parameters
        [Parameter] public int ? Id{get;set;}
        #endregion
        protected int imageResizeWidth = 500;
        protected int imageResizeHeight = 500;
        protected int appSettingUploadFileSize;
        protected long MaxFileSize = 1024 * 1024 * 5;
        protected int maxAllowedFiles = 5;
        private bool IsSaveButtonDisabled = false;
        protected bool errorVisible;
        protected EmailTemplatesDto emailTemplates = new();
      
        IEnumerable<EmailTemplateType> emailTemplateTypes = Enum.GetValues(typeof(EmailTemplateType)).Cast<EmailTemplateType>();
        protected override async Task OnInitializedAsync(){
          
            if(Id.HasValue){
                var response = await EmailTemplateService.GetById(Id.Value);
                if(response.Ok && response.Result != null){
                    emailTemplates = response.Result;
                    if(emailTemplates.Status == 99) IsSaveButtonDisabled = true;
                } else
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
        }
        protected async Task FormSubmit(){
            if(emailTemplates.EmailTemplateType == EmailTemplateType.None){
                NotificationService.Notify(NotificationSeverity.Error, "Lütfen şablon seçiniz");
                return;
            }
            emailTemplates.Id =  Id;
            emailTemplates.Status = 1;
            var submitRs = await EmailTemplateService.Upsert(new AuditWrapDto<EmailTemplatesDto>(){UserId = Security.User.Id, Dto = emailTemplates});
            if(submitRs.Ok){
                DialogService.Close(emailTemplates);
            } else{
                NotificationService.Notify(NotificationSeverity.Error, submitRs.GetMetadataMessages());
            }
        }
        protected void CancelButtonClick(MouseEventArgs args){DialogService.Close(emailTemplates);}
       
      
       
       
    }