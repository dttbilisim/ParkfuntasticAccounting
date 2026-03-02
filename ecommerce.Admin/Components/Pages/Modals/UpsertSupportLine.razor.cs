using ecommerce.Admin.Domain.Dtos.SupportLineDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals;

public partial class UpsertSupportLine
{
    [Inject]
    private DialogService DialogService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Inject]
    private ISupportLineService SupportLineService { get; set; }

    [Parameter]
    public int Id { get; set; }

    private SupportLineUpsertDto SupportLine { get; set; } = new();

    private bool Saving { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var response = await SupportLineService.GetSupportLineById(Id);

        if (!response.Ok)
        {
            NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            return;
        }

        SupportLine = response.Result;

        await InvokeAsync(StateHasChanged);
    }

    private async Task FormSubmit()
    {
        Saving = true;

        var submitRs = await SupportLineService.UpsertSupportLine(SupportLine);

        if (submitRs.Ok)
        {
            DialogService.Close(SupportLine);
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, submitRs.GetMetadataMessages());
        }

        Saving = false;
    }

    private void CancelButtonClick(MouseEventArgs args)
    {
        DialogService.Close();
    }
}