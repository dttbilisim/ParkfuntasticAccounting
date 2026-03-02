using AutoMapper;
using ecommerce.Admin.Domain.Dtos.CurrencyDto;
using ecommerce.Admin.Services;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals;

public partial class UpsertCurrency
{
    #region Injection

    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected DialogService DialogService { get; set; } = default!;
    [Inject] protected NotificationService NotificationService { get; set; } = default!;
    [Inject] public ICurrencyAdminService Service { get; set; } = default!;
    [Inject] public IMapper Mapper { get; set; } = default!;
    [Inject] protected AuthenticationService Security { get; set; } = default!;

    #endregion

    [Parameter] public int? Id { get; set; }

    private bool IsSaveButtonDisabled;
    protected bool errorVisible;
    protected CurrencyUpsertDto currency = new();
    public bool Status { get; set; } = true;

    protected override async Task OnInitializedAsync()
    {
        if (Id.HasValue)
        {
            var response = await Service.GetCurrencyById(Id.Value);
            if (response.Ok && response.Result != null)
            {
                currency = response.Result;
                Status = currency.Status == (int)EntityStatus.Passive || currency.Status == (int)EntityStatus.Deleted
                    ? false
                    : true;

                if (currency.Status == EntityStatus.Deleted.GetHashCode())
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
            currency.Id = Id;
            currency.StatusBool = Status;

            var submitRs = await Service.UpsertCurrency(new Core.Helpers.AuditWrapDto<CurrencyUpsertDto>
            {
                UserId = Security.User.Id,
                Dto = currency
            });

            if (submitRs.Ok)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Başarılı",
                    Detail = Id.HasValue ? "Kur başarıyla güncellendi." : "Kur başarıyla eklendi."
                });
                DialogService.Close(currency);
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


