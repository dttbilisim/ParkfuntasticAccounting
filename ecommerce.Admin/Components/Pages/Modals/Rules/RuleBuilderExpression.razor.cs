using ecommerce.Admin.Components.Layout;
using ecommerce.Admin.Domain.Dtos.RulesDto;
using ecommerce.Core.Rules.Fields;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals.Rules;

public partial class RuleBuilderExpression : OwningComponentBase
{
    [Parameter]
    public RuleUpsertDto Expression { get; set; } = new();

    [Parameter]
    public EventCallback<RuleUpsertDto> ExpressionChanged { get; set; }

    [Parameter]
    public EventCallback<RuleUpsertDto> OnDelete { get; set; }

    [CascadingParameter(Name = nameof(FieldScopeDefinition))]
    public FieldScopeDefinition FieldScopeDefinition { get; set; }

    [CascadingParameter(Name = nameof(RuleValidator))]
    public RuleValidator RuleValidator { get; set; }

    private RadzenFluentValidator<RuleUpsertDto> RuleFluentValidator { get; set; }

    private List<FieldDefinitionValueSelectListOption> SelectListData { get; set; } = new();
    private int SelectListDataCount { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (Expression.Field != null)
        {
            Expression.FieldDefinition = FieldScopeDefinition.GetFieldOrNull(Expression.Field);
        }

        if (Expression.FieldDefinition?.SelectList != null && (Expression.Values != null || Expression.Value != null))
        {
            await LoadSelectListData(new LoadDataArgs(), Expression.Values ?? new List<string> { Expression.Value! });
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task RemoveExpression()
    {
        await OnDelete.InvokeAsync(Expression);
    }

    private void FieldChanged()
    {
        Expression.FieldDefinition = Expression.Field != null ? FieldScopeDefinition.GetFieldOrNull(Expression.Field) : null;

        Expression.Operator = null;
        Expression.Value = null;
        Expression.Values = null;

        StateHasChanged();
    }

    private void OperatorChanged()
    {
        Expression.Values = null;

        StateHasChanged();
    }

    private void AddArrayValue()
    {
        if (string.IsNullOrEmpty(Expression.Value))
        {
            return;
        }

        Expression.Values ??= new List<string>();
        if (!Expression.Values.Contains(Expression.Value))
        {
            Expression.Values.Add(Expression.Value);
        }

        Expression.Value = null;
    }

    private async Task LoadSelectListData(LoadDataArgs args)
    {
        await LoadSelectListData(args, null);
    }

    private async Task LoadSelectListData(LoadDataArgs args, IEnumerable<string>? selectedItems)
    {
        var selectList = Expression.FieldDefinition?.SelectList;

        if (selectList == null)
        {
            return;
        }

        SelectListData = selectList.Options.ToList();
        SelectListDataCount = SelectListData.Count;

        if (selectList.OptionProviderType != null
            && ScopedServices.GetService(selectList.OptionProviderType) is IFieldDefinitionValueOptionProvider optionProvider)
        {
            var options = await optionProvider.GetAsync(
                Expression.FieldDefinition!,
                args.Skip ?? 0,
                args.Top ?? 10,
                args.Filter,
                selectedItems?.ToArray()
            );

            SelectListData = options.Data.ToList();
            SelectListDataCount = options.Count > 0 ? options.Count : SelectListData.Count;
        }
    }

    private void SelectListValueChanged(object value, bool isMultiple)
    {
        if (isMultiple)
        {
            Expression.Values = (value as IEnumerable<string>)?.ToList();
            Expression.Value = null;
        }
        else
        {
            Expression.Value = FieldValueDictionary.ChangeTypeAs<string?>(value);
            Expression.Values = null;
        }
    }
}