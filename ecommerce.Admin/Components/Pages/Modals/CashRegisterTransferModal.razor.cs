using ecommerce.Admin.Domain.Dtos.CashRegisterMovementDto;
using ecommerce.Admin.Domain.Dtos.CashRegisterDto;
using ecommerce.Admin.Services;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Helpers;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class CashRegisterTransferModal
    {
        [Parameter]
        public List<CashRegisterListDto> CashRegisterList { get; set; } = new();

        [Inject] protected DialogService DialogService { get; set; } = null!;
        [Inject] protected NotificationService NotificationService { get; set; } = null!;
        [Inject] protected AuthenticationService Security { get; set; } = null!;
        [Inject] protected ICashRegisterMovementService MovementService { get; set; } = null!;

        protected CashRegisterTransferDto Model { get; set; } = new()
        {
            TransactionDate = DateTime.Now
        };

        protected bool Saving { get; set; }

        /// <summary>Hedef kasa listesi: kaynak dışında, aynı dövizde olanlar.</summary>
        protected List<CashRegisterListDto> FilteredTargetCashRegisters
        {
            get
            {
                if (Model.SourceCashRegisterId == 0) return new List<CashRegisterListDto>();
                var source = CashRegisterList.FirstOrDefault(c => c.Id == Model.SourceCashRegisterId);
                if (source == null) return new List<CashRegisterListDto>();
                return CashRegisterList
                    .Where(c => c.Id != Model.SourceCashRegisterId && c.CurrencyId == source.CurrencyId)
                    .ToList();
            }
        }

        protected string CurrencyDisplay
        {
            get
            {
                if (Model.SourceCashRegisterId == 0) return "—";
                var source = CashRegisterList.FirstOrDefault(c => c.Id == Model.SourceCashRegisterId);
                return source?.CurrencyCode ?? "—";
            }
        }

        protected void OnSourceCashRegisterChange()
        {
            var source = CashRegisterList.FirstOrDefault(c => c.Id == Model.SourceCashRegisterId);
            if (source != null)
            {
                Model.CurrencyId = source.CurrencyId;
                if (!FilteredTargetCashRegisters.Any(x => x.Id == Model.TargetCashRegisterId))
                    Model.TargetCashRegisterId = 0;
            }
            else
            {
                Model.CurrencyId = 0;
                Model.TargetCashRegisterId = 0;
            }
        }

        protected async Task FormSubmit()
        {
            if (Model.SourceCashRegisterId == 0 || Model.TargetCashRegisterId == 0)
            {
                NotificationService.Notify(NotificationSeverity.Warning, "Uyarı", "Kaynak ve hedef kasa seçiniz.");
                return;
            }

            try
            {
                Saving = true;
                var response = await MovementService.CreateTransfer(new AuditWrapDto<CashRegisterTransferDto>
                {
                    UserId = Security.User.Id,
                    Dto = Model
                });

                if (response.Ok)
                {
                    NotificationService.Notify(NotificationSeverity.Success, "Başarılı", "Kasalar arası virman oluşturuldu.");
                    DialogService.Close(true);
                }
                else
                    NotificationService.Notify(NotificationSeverity.Error, "Hata", response.GetMetadataMessages());
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Hata", ex.Message);
            }
            finally
            {
                Saving = false;
            }
        }
    }
}
