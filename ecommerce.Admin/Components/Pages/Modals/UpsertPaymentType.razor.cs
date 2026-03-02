using ecommerce.Admin.Domain.Dtos.PaymentTypeDto;
using ecommerce.Admin.Services;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Core.Models;
using ecommerce.Core.Helpers;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertPaymentType
    {
        [Inject] protected DialogService DialogService { get; set; } = null!;
        [Inject] protected NotificationService NotificationService { get; set; } = null!;
        [Inject] protected AuthenticationService Security { get; set; } = null!;
        [Inject] public IPaymentTypeService PaymentTypeService { get; set; } = null!;

        [Parameter] public int? Id { get; set; }

        protected PaymentTypeUpsertDto? Model { get; set; }
        protected bool Saving { get; set; }

        protected override async Task OnInitializedAsync()
        {
            if (Id.HasValue && Id.Value > 0)
            {
                var response = await PaymentTypeService.GetPaymentTypeById(Id.Value);
                if (response.Ok && response.Result != null)
                {
                    Model = response.Result;
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, "Hata", "Ödeme tipi yüklenemedi.");
                    DialogService.Close(false);
                }
            }
            else
            {
                Model = new PaymentTypeUpsertDto
                {
                    IsActive = true
                };
            }
        }

        protected async Task FormSubmit(PaymentTypeUpsertDto args)
        {
            try
            {
                Saving = true;
                var response = await PaymentTypeService.UpsertPaymentType(new AuditWrapDto<PaymentTypeUpsertDto>
                {
                    UserId = Security.User.Id,
                    Dto = args
                });

                if (response.Ok)
                {
                    NotificationService.Notify(NotificationSeverity.Success, "Başarılı", "Ödeme tipi kaydedildi.");
                    DialogService.Close(true);
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, "Hata", response.GetMetadataMessages());
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Sistem Hatası", ex.Message);
            }
            finally
            {
                Saving = false;
            }
        }
    }
}
