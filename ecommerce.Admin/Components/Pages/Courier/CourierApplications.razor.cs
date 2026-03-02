using ecommerce.Admin.Domain.Dtos.CourierApplicationDto;
using ecommerce.Admin.Services;
using ecommerce.Admin.Services.Dtos;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages.Courier;

public partial class CourierApplications
{
    [Inject] protected ICourierApplicationService ApplicationService { get; set; } = null!;
    [Inject] protected DialogService DialogService { get; set; } = null!;
    [Inject] protected NotificationService NotificationService { get; set; } = null!;
    [Inject] protected AuthenticationService Security { get; set; } = null!;
    [Inject] protected IConfiguration Configuration { get; set; } = null!;

    protected List<CourierApplicationListDto>? Items { get; set; }
    protected int Count { get; set; }
    protected RadzenDataGrid<CourierApplicationListDto>? Grid;

    protected CourierApplicationStatus? FilterStatus { get; set; }

    protected List<SelectItemDto<CourierApplicationStatus?>> StatusOptions { get; set; } = new()
    {
        new SelectItemDto<CourierApplicationStatus?> { Text = "Beklemede", Value = CourierApplicationStatus.Pending },
        new SelectItemDto<CourierApplicationStatus?> { Text = "Onaylandı", Value = CourierApplicationStatus.Approved },
        new SelectItemDto<CourierApplicationStatus?> { Text = "Reddedildi", Value = CourierApplicationStatus.Rejected }
    };

    private PageSetting _pager = new();
    private bool _initialLoadDone;
    private string? _documentApiBaseUrl;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_initialLoadDone)
        {
            _initialLoadDone = true;
            _documentApiBaseUrl = Configuration["AppSettings:ApiBaseUrl"] ?? Configuration["CourierDocumentApiBaseUrl"];
            await LoadData(new LoadDataArgs { Skip = 0, Top = 25 });
        }
    }

    protected async Task LoadData(LoadDataArgs args)
    {
        try
        {
            _pager = new PageSetting(args.Filter, args.OrderBy, args.Skip, args.Top ?? 25);
            var response = await ApplicationService.GetPaged(_pager, FilterStatus);

            if (response.Ok && response.Result != null)
            {
                Items = response.Result.Data ?? new List<CourierApplicationListDto>();
                Count = response.Result.DataCount;
            }
            else
            {
                Items = new List<CourierApplicationListDto>();
                Count = 0;
                if (!response.Ok)
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Hata", ex.Message);
            Items = new List<CourierApplicationListDto>();
            Count = 0;
        }
        await InvokeAsync(StateHasChanged);
    }

    protected async Task ApplyFilter()
    {
        if (Grid != null)
            await Grid.FirstPage(true);
    }

    protected async Task ClearFilter()
    {
        FilterStatus = null;
        if (Grid != null)
            await Grid.FirstPage(true);
    }

    protected string GetStatusBadgeClass(CourierApplicationStatus status)
    {
        return status switch
        {
            CourierApplicationStatus.Pending => "bg-warning text-dark",
            CourierApplicationStatus.Approved => "bg-success",
            CourierApplicationStatus.Rejected => "bg-danger",
            _ => "bg-secondary"
        };
    }

    protected async Task ShowDetail(CourierApplicationListDto item)
    {
        await DialogService.OpenAsync<ecommerce.Admin.Components.Pages.Modals.CourierApplicationDetailModal>("Başvuru Detayı",
            new Dictionary<string, object>
            {
                { "UserName", item.UserName },
                { "Email", item.Email ?? "" },
                { "Phone", item.Phone },
                { "IdentityNumber", item.IdentityNumber },
                { "Note", item.Note },
                { "TaxNumber", item.TaxNumber },
                { "TaxOffice", item.TaxOffice },
                { "IBAN", item.IBAN },
                { "TaxPlatePath", item.TaxPlatePath },
                { "SignatureDeclarationPath", item.SignatureDeclarationPath },
                { "IdCopyPath", item.IdCopyPath },
                { "CriminalRecordPath", item.CriminalRecordPath },
                { "ApplicationId", item.Id },
                { "DocumentApiBaseUrl", _documentApiBaseUrl ?? "" },
            },
            new DialogOptions { Width = "500px", Resizable = true, Draggable = true });
    }

    protected async Task ReviewRow(CourierApplicationListDto item, bool approve)
    {
        var reviewDto = new CourierApplicationReviewDto { Id = item.Id, Approve = approve };
        if (!approve)
        {
            var result = await DialogService.OpenAsync<ecommerce.Admin.Components.Pages.Modals.ReviewCourierApplicationModal>("Başvuruyu Reddet",
                new Dictionary<string, object> { { "ApplicationId", item.Id }, { "UserName", item.UserName } },
                new DialogOptions { Width = "420px", Resizable = true, Draggable = true });
            if (result is CourierApplicationReviewDto dto)
                reviewDto = dto;
            else
                return;
        }

        var response = await ApplicationService.Review(reviewDto.Id, reviewDto, Security.User.Id);
        if (response.Ok)
        {
            NotificationService.Notify(NotificationSeverity.Success, approve ? "Başvuru onaylandı." : "Başvuru reddedildi.");
            await LoadData(new LoadDataArgs { Skip = _pager.Skip ?? 0, Top = _pager.Take ?? 25 });
            if (Grid != null)
                await Grid.Reload();
        }
        else
            NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
    }

    protected async Task DeleteRow(CourierApplicationListDto item)
    {
        if (await DialogService.Confirm(
                $"\"{item.UserName}\" başvurusunu tamamen silmek istediğinize emin misiniz? Bu işlem geri alınamaz.",
                "Kurye Başvurusunu Sil",
                new ConfirmOptions { OkButtonText = "Evet", CancelButtonText = "Hayır" }) != true)
            return;

        var response = await ApplicationService.Delete(item.Id);
        if (response.Ok)
        {
            NotificationService.Notify(NotificationSeverity.Success, "Başvuru silindi.");
            await LoadData(new LoadDataArgs { Skip = _pager.Skip ?? 0, Top = _pager.Take ?? 25 });
            if (Grid != null)
                await Grid.Reload();
        }
        else
            NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
    }
}
