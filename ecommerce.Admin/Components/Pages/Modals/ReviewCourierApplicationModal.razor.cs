using ecommerce.Admin.Domain.Dtos.CourierApplicationDto;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals;

public partial class ReviewCourierApplicationModal
{
    [Parameter] public int ApplicationId { get; set; }
    [Parameter] public string UserName { get; set; } = "";
    [Inject] protected DialogService DialogService { get; set; } = null!;

    protected CourierApplicationReviewDto Model { get; set; } = new();

    protected override void OnInitialized()
    {
        Model.Id = ApplicationId;
        Model.Approve = false;
    }

    private void OnSubmit()
    {
        DialogService.Close(Model);
    }
}
