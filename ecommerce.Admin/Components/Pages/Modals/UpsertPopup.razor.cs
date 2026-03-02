using AutoMapper;
using ecommerce.Admin.Components.Layout;
using ecommerce.Admin.CustomComponents.Modals;
using ecommerce.Admin.Domain.Dtos.PopupDto;
using ecommerce.Admin.Domain.Dtos.RulesDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Resources;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertPopup
    {
        [Inject]
        private DialogService DialogService { get; set; }

        [Inject]
        private NotificationService NotificationService { get; set; }

        [Inject]
        private IMapper Mapper { get; set; }

        [Inject]
        private IStringLocalizer<Culture_TR> RadzenLocalizer { get; set; }

        [Inject]
        public IPopupService PopupService { get; set; }

        [Inject]
        private IHostEnvironment Environment { get; set; }

        [Inject]
        private IValidator<PopupUpsertDto> PopupValidator { get; set; }

        [Parameter]
        public int? Id { get; set; }

        private PopupUpsertDto Popup = new();

        private bool Saving { get; set; }

        private RadzenFluentValidator<PopupUpsertDto> PopupFluentValidator { get; set; }

        protected override async Task OnInitializedAsync()
        {
            if (Id.HasValue)
            {
                var response = await PopupService.GetPopupById(Id.Value);

                if (response.Ok)
                {
                    Popup = response.Result;
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                }
            }

            Popup.Rule ??= new RuleUpsertDto();
        }

        private async Task FormSubmit()
        {
            Saving = true;

            if (Popup.Rule is { Field: null })
            {
                Popup.Rule = null;
            }

            var submitRs = await PopupService.UpsertPopup(Popup);

            if (submitRs.Ok)
            {
                DialogService.Close(Popup);
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, submitRs.GetMetadataMessages());
            }

            Saving = false;
        }

        private async Task ShowErrors()
        {
            await DialogService.OpenAsync<ValidationModal>(
                "Uyari",
                new Dictionary<string, object>
                {
                    { "Errors", PopupFluentValidator.GetValidationMessages().Select(p => new Dictionary<string, string> { { p.Key, p.Value } }).ToList() }
                }
            );
        }

        private void CancelButtonClick(MouseEventArgs args)
        {
            DialogService.Close();
        }

        private void PopupTriggerChanged()
        {
            if (Popup.Trigger == PopupTrigger.Auto)
            {
                Popup.TriggerReference = null;
            }
            else
            {
                Popup.TimeExpire = 0;
            }
        }
    }
}