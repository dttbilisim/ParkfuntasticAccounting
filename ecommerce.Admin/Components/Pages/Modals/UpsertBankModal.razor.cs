using ecommerce.Domain.Shared.Dtos.Bank.BankDto;
using ecommerce.Admin.Services;
using ecommerce.Core.Helpers;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Abstract;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertBankModal
    {
        [Inject] protected IBankService BankService { get; set; }
        [Inject] protected DialogService DialogService { get; set; }
        [Inject] protected NotificationService NotificationService { get; set; }
        [Inject] protected AuthenticationService Security { get; set; }

        [Parameter] public int? Id { get; set; }

        protected BankUpsertDto model = new();

        protected override async Task OnInitializedAsync()
        {
            if (Id.HasValue)
            {
                var response = await BankService.GetBankById(Id.Value);
                if (response.Ok && response.Result != null)
                {
                    model = response.Result;
                }
                else if (response.Exception != null)
                {
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                }
            }
        }

        protected async Task Save()
        {
            var response = await BankService.UpsertBank(new AuditWrapDto<BankUpsertDto>()
            {
                UserId = Security.User.Id,
                Dto = model
            });

            if (response.Ok)
            {
                NotificationService.Notify(NotificationSeverity.Success, "Banka başarıyla kaydedildi.");
                DialogService.Close(true);
            }
            else if (response.Exception != null)
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
        }

        protected void Close()
        {
            DialogService.Close(false);
        }
    }
}
