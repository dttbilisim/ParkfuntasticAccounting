using AutoMapper;
using ecommerce.Admin.Domain.Dtos.SearchSynonymDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using ecommerce.Core.Helpers;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertSearchSynonym
    {
        #region Injections

        [Inject]
        protected DialogService DialogService { get; set; }

        [Inject]
        protected NotificationService NotificationService { get; set; }

        [Inject]
        public ISearchSynonymAdminService Service { get; set; }

        [Inject]
        protected AuthenticationService Security { get; set; }

        #endregion

        #region Parameters

        [Parameter]
        public int? Id { get; set; }

        #endregion

        protected bool errorVisible;
        protected string errorMessage = "Eş anlamlı kaydedilemedi!";
        protected SearchSynonymUpsertDto dto = new();
        protected IEnumerable<object> categories = Enum.GetValues(typeof(ecommerce.Core.Entities.SearchSynonymCategory))
            .Cast<ecommerce.Core.Entities.SearchSynonymCategory>()
            .Select(v => new { Text = v.GetEnumDisplayName(), Value = v });

        protected override async Task OnInitializedAsync()
        {
            if (Id.HasValue && Id.Value > 0)
            {
                var response = await Service.GetSynonymById(Id.Value);
                if (response.Ok && response.Result != null)
                {
                    dto = response.Result;
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                }
            }
            else
            {
                dto = new SearchSynonymUpsertDto
                {
                    StatusBool = true,
                    IsBidirectional = true
                };
            }
        }

        protected async Task FormSubmit()
        {
            try
            {
                var auditDto = new AuditWrapDto<SearchSynonymUpsertDto>
                {
                    UserId = Security.User.Id,
                    Dto = dto
                };

                var response = await Service.UpsertSynonym(auditDto);
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
