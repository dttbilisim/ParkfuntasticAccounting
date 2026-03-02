using ecommerce.Domain.Shared.Dtos.Bank.BankCardDto;
using ecommerce.Domain.Shared.Dtos.Bank.BankCreditCardInstallmentDto;
using ecommerce.Domain.Shared.Abstract;
using ecommerce.Admin.Services;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertBankCreditCardInstallmentModal
    {
        [Inject] protected IBankService BankService { get; set; }
        [Inject] protected DialogService DialogService { get; set; }
        [Inject] protected NotificationService NotificationService { get; set; }
        [Inject] protected AuthenticationService Security { get; set; }

        [Parameter] public int? Id { get; set; }

        protected BankCreditCardInstallmentUpsertDto model = new();
        protected List<BankCardListDto> bankCards = new();

        protected override async Task OnInitializedAsync()
        {
            var bankCardsResponse = await BankService.GetBankCards(new PageSetting("", "Id desc", 0, 1000));
            if (bankCardsResponse.Ok && bankCardsResponse.Result?.Data != null)
            {
                bankCards = bankCardsResponse.Result.Data;
            }

            if (Id.HasValue)
            {
                var response = await BankService.GetBankCreditCardInstallmentById(Id.Value);
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
            var response = await BankService.UpsertBankCreditCardInstallment(new AuditWrapDto<BankCreditCardInstallmentUpsertDto>()
            {
                UserId = Security.User.Id,
                Dto = model
            });

            if (response.Ok)
            {
                NotificationService.Notify(NotificationSeverity.Success, "Taksit bilgisi başarıyla kaydedildi.");
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
