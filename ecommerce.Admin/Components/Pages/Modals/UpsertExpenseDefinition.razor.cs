using ecommerce.Admin.Domain.Dtos.ExpenseDto;
using ecommerce.Admin.Services;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Extensions;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals;

public partial class UpsertExpenseDefinition
{
    #region Injection
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected DialogService DialogService { get; set; } = default!;
    [Inject] protected NotificationService NotificationService { get; set; } = default!;
    [Inject] protected AuthenticationService Security { get; set; } = default!;
    [Inject] public IExpenseDefinitionService ExpenseService { get; set; } = default!;
    #endregion

    [Parameter] public int? Id { get; set; }
    [Parameter] public int? ParentId { get; set; }
    [Parameter] public int? OperationType { get; set; }
    // Ana gider mi? true: işlem tipi seçilir, parent seçilmez.
    // Alt gider mi? false: işlem tipi gizli, parent (Ana İşlem) zorunlu.
    [Parameter] public bool IsMainOperation { get; set; }

    protected ExpenseDefinitionUpsertDto expense = new();
    protected List<ExpenseDefinitionListDto> MainOptions { get; set; } = new();
    protected IEnumerable<ExpenseOperationType> OperationTypeOptions = Enum.GetValues<ExpenseOperationType>();

    protected override async Task OnInitializedAsync()
    {
        expense = new ExpenseDefinitionUpsertDto
        {
            Id = Id,
            OperationType = OperationType.HasValue ? (ExpenseOperationType)OperationType.Value : ExpenseOperationType.Gider,
            ParentId = ParentId
        };

        if (Id.HasValue)
        {
            var rs = await ExpenseService.GetExpenseById(Id.Value);
            if (rs.Ok && rs.Result != null)
            {
                expense = rs.Result;
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, rs.GetMetadataMessages());
            }
        }

        // Ana işlemler dropdown'u için sadece ParentId == null kayıtları yükle
        await LoadMainOptionsAsync();
    }

    private async Task LoadMainOptionsAsync()
    {
        var mainRs = await ExpenseService.GetMainExpenses(expense.OperationType);
        if (mainRs.Ok && mainRs.Result != null)
        {
            MainOptions = mainRs.Result;
        }
    }

    private async Task OperationTypeChanged(ExpenseOperationType newValue)
    {
        expense.OperationType = newValue;
        await LoadMainOptionsAsync();
    }

    protected async Task FormSubmit()
    {
        try
        {
            var submitRs = await ExpenseService.UpsertExpense(new Core.Helpers.AuditWrapDto<ExpenseDefinitionUpsertDto>
            {
                UserId = Security.User.Id,
                Dto = expense
            });

            if (submitRs.Ok)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Başarılı",
                    Detail = "Gider tanımı kaydedildi."
                });
                DialogService.Close(expense);
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, submitRs.GetMetadataMessages());
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, ex.ToString());
        }
    }

    protected void CancelButtonClick(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
    {
        DialogService.Close(null);
    }
}


