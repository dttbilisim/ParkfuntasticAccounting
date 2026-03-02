using ecommerce.Admin.Domain.Dtos.EducationDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Components.Pages.Modals;
using ecommerce.Admin.Services;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;
namespace ecommerce.Admin.Components.Pages;
public partial class EducationCategory{
    [Inject]
    protected IJSRuntime JSRuntime { get; set; }

    [Inject]
    protected NavigationManager NavigationManager { get; set; }

    [Inject]
    protected DialogService DialogService { get; set; }

    [Inject]
    protected TooltipService TooltipService { get; set; }
    [Inject]    public IEducationService  educationService { get; set; }
    [Inject]   protected AuthenticationService Security { get; set; }


    [Inject]
    protected ContextMenuService ContextMenuService { get; set; }

    [Inject]
    protected NotificationService NotificationService { get; set; }
    int count;
    protected EducationCategoryUpsertDto educationcategory = new();
    protected List<EducationCategoryListDto> categories = null;

    protected Radzen.Blazor.RadzenDataGrid<EducationCategoryListDto>? grid0 = new();
    private PageSetting pager;
    private new DialogOptions DialogOptions = new() { Width = "1200px" };
    protected async Task AddButtonClick(MouseEventArgs args)
    {
        await DialogService.OpenAsync<UpsertEducationCategory>("Eğitim Kategori Ekle/Düzenle", null, DialogOptions);
        await grid0.Reload();
    }
    protected async Task EditRow(EducationCategoryListDto args)
    {
        await DialogService.OpenAsync<UpsertEducationCategory>("Kategori Düzenle", new Dictionary<string, object> { { "Id", args.Id } }, DialogOptions);
        await grid0.Reload();
    }
    private async Task LoadData(LoadDataArgs args)
    {
        pager = new PageSetting(args.Filter, args.OrderBy, args.Skip, args.Top);

        var response = await educationService.GetCategories(pager);
        if (response.Ok && response.Result != null)
        {
            categories = response.Result.Data == null ? new List<EducationCategoryListDto>() : response.Result.Data.ToList();
            count = response.Result.DataCount;
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
        }
        StateHasChanged();
    }
    void RowRender(RowRenderEventArgs<EducationCategoryListDto> args)
    {
        if (args.Data.Status == 0)
            args.Attributes.Add("style", $"background-color: {(args.Data.Status == 0 ? "#FFEFEF" : "White")};");
    }
    protected async Task GridDeleteButtonClick(MouseEventArgs args, EducationCategoryListDto category)
    {

        try
        {
            if (await DialogService.Confirm("Seçilen Kategoriyi silmek istediğinize emin misiniz?", "Kayıt Sil", new ConfirmOptions()
            {
                OkButtonText = "Evet",
                CancelButtonText = "Hayır"
            }) == true)
            {
                var deleteResult = await educationService.DeleteCategory(new Core.Helpers.AuditWrapDto<EducationCategoryDeleteDto>()
                {
                    UserId = Security.User.Id,
                    Dto = new EducationCategoryDeleteDto() { Id = (int)category.Id }
                });

                if (deleteResult != null)
                {
                    if (deleteResult != null)
                    {
                        if (deleteResult.Ok)
                            await grid0.Reload();
                        else
                            await DialogService.Alert(deleteResult.Metadata.Message, "Uyarı", new AlertOptions() { OkButtonText = "Tamam" });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = $"Error",
                Detail = $"Unable to delete Category"
            });
        }
    }


}
