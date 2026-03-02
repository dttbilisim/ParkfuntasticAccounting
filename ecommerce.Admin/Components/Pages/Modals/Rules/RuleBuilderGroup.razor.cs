using ecommerce.Admin.Domain.Dtos.RulesDto;
using ecommerce.Core.Rules;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages.Modals.Rules;

public partial class RuleBuilderGroup
{
    [Parameter]
    public RuleUpsertDto GroupExpression { get; set; } = new() { IsGroup = true };

    [Parameter]
    public EventCallback<RuleUpsertDto> GroupExpressionChanged { get; set; }

    [Parameter]
    public EventCallback<RuleUpsertDto> OnDelete { get; set; }

    protected override void OnParametersSet()
    {
        GroupExpression.IsGroup = true;

        foreach (var expression in GroupExpression.Children)
        {
            if (expression.GroupOperator.HasValue)
            {
                expression.IsGroup = true;
            }
        }

        SetGroupOperatorEnabled();
    }

    private void AddGroupOrExpression(RadzenSplitButtonItem item)
    {
        if (item.Value == "Group")
        {
            AddGroup();
        }
        else
        {
            AddExpression();
        }
    }

    private void AddGroup()
    {
        GroupExpression.Children.Add(
            new RuleUpsertDto
            {
                IsGroup = true,
            }
        );

        SetGroupOperatorEnabled();
    }

    private void AddExpression()
    {
        GroupExpression.Children.Add(new RuleUpsertDto());

        SetGroupOperatorEnabled();
    }

    private void RemoveExpression(RuleUpsertDto expression)
    {
        GroupExpression.Children.Remove(expression);

        SetGroupOperatorEnabled();
    }

    private async Task RemoveGroup()
    {
        await OnDelete.InvokeAsync(GroupExpression);
    }

    private void SetGroupOperatorEnabled()
    {
        GroupExpression.IsGroupOperatorEnabled = GroupExpression.Children.Any();

        if (!GroupExpression.GroupOperator.HasValue)
        {
            SetGroupOperator(RuleGroupOperator.And);
        }
    }

    private void SetGroupOperator(RuleGroupOperator? ruleGroupOperator)
    {
        GroupExpression.GroupOperator = ruleGroupOperator;
    }
}