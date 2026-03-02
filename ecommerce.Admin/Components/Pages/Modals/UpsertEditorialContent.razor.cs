using AutoMapper;
using ecommerce.Admin.Components.Layout;
using ecommerce.Admin.CustomComponents.Modals;
using ecommerce.Admin.Domain.Dtos.EditorialContentDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Helpers.Concretes;
using ecommerce.Core.Helpers;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals;

public partial class UpsertEditorialContent
{
    [Inject]
    private DialogService DialogService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Inject]
    private IMapper Mapper { get; set; }

    [Inject]
    private IFileService FileService { get; set; }

    [Inject]
    private FileHelper FileHelper { get; set; }

    [Inject]
    private IEditorialContentService EditorialContentService { get; set; }

    [Inject]
    private IValidator<EditorialContentUpsertDto> EditorialContentValidator { get; set; }

    [Parameter]
    public int? Id { get; set; }

    private EditorialContentUpsertDto EditorialContent { get; set; } = new();

    private bool Saving { get; set; }

    private RadzenFluentValidator<EditorialContentUpsertDto> EditorialContentFluentValidator { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (Id.HasValue)
        {
            var response = await EditorialContentService.GetEditorialContentById(Id.Value);

            if (!response.Ok)
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                return;
            }

            EditorialContent = response.Result;
        }

        await InvokeAsync(StateHasChanged);
    }

    private void ContentTypeChanged()
    {
        if (EditorialContent.Type == EditorialContentType.Article)
        {
            EditorialContent.Video = null;
        }
        else
        {
            EditorialContent.Category = null;
        }
    }

    private async Task FormSubmit()
    {
        Saving = true;

        var submitRs = await EditorialContentService.UpsertEditorialContent(EditorialContent);

        if (submitRs.Ok)
        {
            DialogService.Close(EditorialContent);
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, submitRs.GetMetadataMessages());
        }

        Saving = false;
    }

    private async Task SaveThumbnailFile(IBrowserFile file)
    {
        var fileResponse = await FileService.UploadFile(file, "EditorialContent");

        if (!fileResponse.Ok)
        {
            NotificationService.Notify(NotificationSeverity.Error, fileResponse.GetMetadataMessages());
            return;
        }

        EditorialContent.Thumbnail = fileResponse.Result.Root;
    }

    private async Task ShowErrors()
    {
        await DialogService.OpenAsync<ValidationModal>(
            "Uyari",
            new Dictionary<string, object>
            {
                { "Errors", EditorialContentFluentValidator.GetValidationMessages().Select(p => new Dictionary<string, string> { { p.Key, p.Value } }).ToList() }
            }
        );
    }

    private void CancelButtonClick(MouseEventArgs args)
    {
        DialogService.Close();
    }
}