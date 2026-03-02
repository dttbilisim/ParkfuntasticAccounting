using AutoMapper;
using ecommerce.Admin.Components.Layout;
using ecommerce.Admin.CustomComponents.Modals;
using ecommerce.Admin.Domain.Dtos.SurveyDto;
using ecommerce.Admin.Domain.Dtos.HierarchicalDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Admin.Resources;
using ecommerce.Core.Utils.ResultSet;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using Radzen;
using Radzen.Blazor;
using ecommerce.Admin.Services;

namespace ecommerce.Admin.Components.Pages.Modals;

public partial class UpsertSurvey
{
    [Inject]
    protected AuthenticationService Security { get; set; }

    [Inject]
    private DialogService DialogService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Inject]
    private IMapper Mapper { get; set; }

    [Inject]
    private IStringLocalizer<Culture_TR> RadzenLocalizer { get; set; }

    [Inject]
    private ISurveyService SurveyService { get; set; }

    [Inject]
    public ICorporationService CorporationService { get; set; }

    [Inject]
    public IBranchService BranchService { get; set; }

    [Inject]
    private IValidator<SurveyUpsertDto> SurveyValidator { get; set; }

    [Parameter]
    public int? Id { get; set; }

    private SurveyUpsertDto Survey { get; set; } = new();

    private bool Saving { get; set; }

    private RadzenFluentValidator<SurveyUpsertDto> SurveyFluentValidator { get; set; }

    private RadzenDataGrid<SurveyOptionUpsertDto> SurveyOptionDataGrid { get; set; } = null!;
    private RadzenDataGrid<SurveyAnswerStatisticDto> SurveyAnwerDataGrid { get; set; } = null!;

    private SurveyOptionUpsertDto? SurveyOptionToEdit { get; set; }
    private List<SurveyAnswerStatisticDto> SurveyAnswerStatisticDto { get; set; } = new();

    protected List<CorporationListDto> corporations = new();
    protected List<BranchListDto> branches = new();
    protected int? SelectedCorporationId;

    protected override async Task OnInitializedAsync()
    {
        var corpRs = await CorporationService.GetAllActiveCorporations();
        if (corpRs.Ok) corporations = corpRs.Result;

        if (Id.HasValue)
        {
            var response = await SurveyService.GetSurveyById(Id.Value);

            if (!response.Ok)
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                return;
            }

            Survey = response.Result;

            if (Survey.BranchId.HasValue)
            {
                var allBranchesRs = await BranchService.GetAllActiveBranches();
                if (allBranchesRs.Ok)
                {
                    var currentBranch = allBranchesRs.Result.FirstOrDefault(b => b.Id == Survey.BranchId.Value);
                    if (currentBranch != null)
                    {
                        SelectedCorporationId = currentBranch.CorporationId;
                        branches = allBranchesRs.Result.Where(b => b.CorporationId == SelectedCorporationId).ToList();
                    }
                }
            }

            var answer = await SurveyService.GetSurveyAnswerStatistics(Id.Value);
            if (answer.Ok)
            {
                SurveyAnswerStatisticDto = answer.Result;
            }
        }

        await InvokeAsync(StateHasChanged);
    }

    protected async Task OnCorporationChange()
    {
        Survey.BranchId = null;
        if (SelectedCorporationId.HasValue)
        {
            var branchRs = await BranchService.GetAllActiveBranches();
            if (branchRs.Ok)
            {
                branches = branchRs.Result.Where(b => b.CorporationId == SelectedCorporationId.Value).ToList();
            }
        }
        else
        {
            branches = new();
        }
    }

    private void OnRadzenGridRender<TItem>(DataGridRenderEventArgs<TItem> args)
    {
        if (!args.FirstRender)
        {
            return;
        }

        _ = SetRadzenTexts(args.Grid);
    }

    private async Task SetRadzenTexts(RadzenComponent radzenComponent)
    {
        var parameters = ParameterView.FromDictionary(
            RadzenLocalizer.GetAllStrings().ToDictionary(l => l.Name, l => (object?) l.Value)
        );

        await radzenComponent.SetParametersAsync(parameters);

        await InvokeAsync(StateHasChanged);
    }

    private async Task FormSubmit()
    {
        Saving = true;

        var submitRs = await SurveyService.UpsertSurvey(new ecommerce.Core.Helpers.AuditWrapDto<SurveyUpsertDto>
        {
            UserId = Security.User.Id,
            Dto = Survey
        });

        if (submitRs.Ok)
        {
            DialogService.Close(Survey);
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, submitRs.GetMetadataMessages());
        }

        Saving = false;
    }

    private async Task ShowErrors()
    {
        await DialogService.OpenAsync<ValidationModal>(
            "Uyari",
            new Dictionary<string, object>
            {
                { "Errors", SurveyFluentValidator.GetValidationMessages().Select(p => new Dictionary<string, string> { { p.Key, p.Value } }).ToList() }
            }
        );
    }

    private void CancelButtonClick(MouseEventArgs args)
    {
        DialogService.Close();
    }

    private async Task InsertSurveyOptionRow()
    {
        if (SurveyOptionToEdit != null)
        {
            SurveyOptionDataGrid.CancelEditRow(SurveyOptionToEdit);
        }

        SurveyOptionToEdit = new SurveyOptionUpsertDto();
        await SurveyOptionDataGrid.InsertRow(SurveyOptionToEdit);
    }

    private async Task EditSurveyOptionRow(SurveyOptionUpsertDto dto)
    {
        SurveyOptionToEdit = Mapper.Map<SurveyOptionUpsertDto>(dto);
        await SurveyOptionDataGrid.EditRow(dto);
    }

    private async Task SaveSurveyOptionRow(SurveyOptionUpsertDto dto)
    {
        if (dto == SurveyOptionToEdit)
        {
            Survey.SurveyOptions.Add(dto);
        }
        else
        {
            Mapper.Map(SurveyOptionToEdit, dto);
        }

        await SurveyOptionDataGrid.UpdateRow(dto);
        await SurveyOptionDataGrid.Reload();
    }

    private async Task DeleteSurveyOptionRow(SurveyOptionUpsertDto dto)
    {
        Survey.SurveyOptions.Remove(dto);
        await SurveyOptionDataGrid.Reload();
    }

    private void CancelSurveyOptionEdit(SurveyOptionUpsertDto dto)
    {
        SurveyOptionDataGrid.CancelEditRow(dto);
    }
}