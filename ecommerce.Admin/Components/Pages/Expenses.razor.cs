using ecommerce.Admin.Domain.Dtos.ExpenseDto;
using ecommerce.Admin.Services;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace ecommerce.Admin.Components.Pages;

public partial class Expenses
{
    #region Injections
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected NotificationService NotificationService { get; set; } = default!;
    [Inject] protected DialogService DialogService { get; set; } = default!;
    [Inject] protected AuthenticationService Security { get; set; } = default!;
    [Inject] public IExpenseDefinitionService ExpenseService { get; set; } = default!;
    #endregion

    private ExpenseOperationType MainOperationType { get; set; } = ExpenseOperationType.Gider;

    private int? SelectedMainIdForSub { get; set; }
    private int? SelectedMainIdForGrid { get; set; }

    protected List<ExpenseDefinitionListDto> MainExpenses { get; set; } = new();
    protected List<ExpenseDefinitionListDto> SubExpenses { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadMainExpensesAsync();
    }

    private async Task LoadMainExpensesAsync()
    {
        var pager = new PageSetting(string.Empty, "Name asc", 0, 1000);
        var rs = await ExpenseService.GetMainExpenses(pager, MainOperationType);
        if (rs.Ok && rs.Result?.Data != null)
        {
            MainExpenses = rs.Result.Data.ToList();
        }
        else
        {
            MainExpenses = new List<ExpenseDefinitionListDto>();
            NotificationService.Notify(NotificationSeverity.Error, rs.GetMetadataMessages());
        }
    }

    private async Task LoadSubExpensesAsync(int mainId)
    {
        var rs = await ExpenseService.GetSubExpenses(mainId);
        if (rs.Ok && rs.Result != null)
        {
            SubExpenses = rs.Result;
        }
        else
        {
            SubExpenses = new List<ExpenseDefinitionListDto>();
        }
    }

    protected async Task AddMainClick()
    {
        try
        {
            var result = await DialogService.OpenAsync<Modals.UpsertExpenseDefinition>(
                "Yeni Ana Gider Tanımı",
                new Dictionary<string, object?>
                {
                    { "Id", null },
                    { "ParentId", null },
                    { "OperationType", (int)MainOperationType },
                    { "IsMainOperation", true }
                },
                new DialogOptions { Width = "600px" });

            if (result != null)
            {
                await LoadMainExpensesAsync();
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, $"Hata: {ex.Message}");
        }
    }

    protected async Task AddSubClick()
    {
        try
        {
            if (SelectedMainIdForGrid == null)
            {
                NotificationService.Notify(NotificationSeverity.Warning, "Lütfen önce bir ana işlem seçin.");
                return;
            }
            var result = await DialogService.OpenAsync<Modals.UpsertExpenseDefinition>(
                "Alt Gider Tanımı Ekle",
                new Dictionary<string, object?>
                {
                    { "Id", null },
                    { "ParentId", SelectedMainIdForGrid },
                    { "OperationType", (int)MainOperationType },
                    { "IsMainOperation", false }
                },
                new DialogOptions { Width = "600px" });

            if (result != null)
            {
                await LoadSubExpensesAsync(SelectedMainIdForGrid.Value);
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, $"Hata: {ex.Message}");
        }
    }

    protected async Task OnMainRowSelect(ExpenseDefinitionListDto main)
    {
        SelectedMainIdForGrid = main.Id;
        SelectedMainIdForSub = main.Id;
        await LoadSubExpensesAsync(main.Id);
    }

    protected async Task EditMainAsync(ExpenseDefinitionListDto item)
    {
        var result = await DialogService.OpenAsync<Modals.UpsertExpenseDefinition>(
            "Ana Gider Tanımı Düzenle",
            new Dictionary<string, object?>
            {
                { "Id", item.Id },
                { "ParentId", null },
                { "OperationType", (int)item.OperationType },
                { "IsMainOperation", true }
            },
            new DialogOptions { Width = "600px" });

        if (result != null)
        {
            await LoadMainExpensesAsync();
            if (SelectedMainIdForGrid.HasValue)
            {
                await LoadSubExpensesAsync(SelectedMainIdForGrid.Value);
            }
        }
    }

    protected async Task EditSubAsync(ExpenseDefinitionListDto item)
    {
        var result = await DialogService.OpenAsync<Modals.UpsertExpenseDefinition>(
            "Alt Gider Tanımı Düzenle",
            new Dictionary<string, object?>
            {
                { "Id", item.Id },
                { "ParentId", item.ParentId },
                { "OperationType", (int)item.OperationType },
                { "IsMainOperation", false }
            },
            new DialogOptions { Width = "600px" });

        if (result != null && item.ParentId.HasValue)
        {
            await LoadSubExpensesAsync(item.ParentId.Value);
        }
    }

    // RowSelect için placeholder (şu anda seçim sadece düzenleme butonuyla yapılıyor)
    protected Task OnSubRowSelect(ExpenseDefinitionListDto sub)
    {
        SelectedMainIdForSub = sub.ParentId;
        return Task.CompletedTask;
    }

    protected async Task DeleteExpenseAsync(int id)
    {
        var confirm = await DialogService.Confirm(
            "Seçilen gider tanımını silmek istediğinize emin misiniz?",
            "Kayıt Sil",
            new ConfirmOptions { OkButtonText = "Evet", CancelButtonText = "Hayır" });

        if (confirm != true)
            return;

        var rs = await ExpenseService.DeleteExpense(new AuditWrapDto<ExpenseDeleteDto>
        {
            UserId = Security.User.Id,
            Dto = new ExpenseDeleteDto { Id = id }
        });

        if (rs.Ok)
        {
            NotificationService.Notify(NotificationSeverity.Success, "Gider tanımı silindi.");
            await LoadMainExpensesAsync();
            if (SelectedMainIdForGrid.HasValue)
            {
                await LoadSubExpensesAsync(SelectedMainIdForGrid.Value);
            }
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, rs.GetMetadataMessages());
        }
    }
}


