using AutoMapper;
using ecommerce.Admin.Domain.Dtos.ActiveArticleDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertActiveArticle
    {
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
        public IActiveArticlesService Service { get; set; }

        [Inject]
        public IMapper Mapper { get; set; }

        [Inject]
        protected AuthenticationService Security { get; set; }


        [Parameter]
        public int? Id { get; set; }

        private bool IsSaveButtonDisabled = false;
        protected bool errorVisible;
        protected ActiveArticleUpsertDto activeArticle = new();
        public bool Status { get; set; } = true;


        protected override async Task OnInitializedAsync()
        {            

            if (Id.HasValue)
            {
                var response = await Service.GetActiveArticleById(Id.Value);
                if (response.Ok && response.Result != null)
                {
                    activeArticle = response.Result;
                    Status = activeArticle.Status == (int)EntityStatus.Passive || activeArticle.Status == (int)EntityStatus.Deleted ? false : true;

                    if (activeArticle.Status == EntityStatus.Deleted.GetHashCode())
                        IsSaveButtonDisabled = true;
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
                activeArticle.Id = Id;
                activeArticle.StatusBool = Status;

                var submitRs = await Service.UpsertActiveArticle(new Core.Helpers.AuditWrapDto<ActiveArticleUpsertDto>()
                {
                    UserId = Security.User.Id,
                    Dto = activeArticle
                });
                if (submitRs.Ok)
                {

                    DialogService.Close(activeArticle);
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
