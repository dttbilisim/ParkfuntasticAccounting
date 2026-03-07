using ecommerce.Admin.Domain.Dtos.SaleOptionsDto;
using ecommerce.Admin.Services;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertSaleOptions
    {
        [Inject] protected DialogService DialogService { get; set; } = null!;
        [Inject] protected NotificationService NotificationService { get; set; } = null!;
        [Inject] protected AuthenticationService Security { get; set; } = null!;
        [Inject] public ISaleOptionsService SaleOptionsService { get; set; } = null!;

        [Parameter] public int? Id { get; set; }

        protected SaleOptionsUpsertDto? Model { get; set; }
        protected bool Saving { get; set; }

        protected override async Task OnInitializedAsync()
        {
            if (Id.HasValue && Id.Value > 0)
            {
                var response = await SaleOptionsService.GetSaleOptionsById(Id.Value);
                if (response.Ok && response.Result != null)
                {
                    Model = response.Result;
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, "Hata", "Satış seçeneği yüklenemedi.");
                    DialogService.Close(false);
                }
            }
            else
            {
                Model = new SaleOptionsUpsertDto();
            }
        }

        protected async Task FormSubmit(SaleOptionsUpsertDto args)
        {
            try
            {
                Saving = true;
                var response = await SaleOptionsService.UpsertSaleOptions(new AuditWrapDto<SaleOptionsUpsertDto>
                {
                    UserId = Security.User.Id,
                    Dto = args
                });

                if (response.Ok)
                {
                    NotificationService.Notify(NotificationSeverity.Success, "Başarılı", "Satış seçeneği kaydedildi.");
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
