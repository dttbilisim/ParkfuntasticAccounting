using ecommerce.Admin.Domain.Dtos.RulesDto;
using ecommerce.Core.Rules.Fields;
using Microsoft.AspNetCore.Components;

namespace ecommerce.Admin.Components.Pages.Modals.Rules;

public partial class RuleBuilder
{
    [Parameter]
    public RuleUpsertDto? MainRule { get; set; }

    [Parameter]
    public EventCallback<RuleUpsertDto> MainRuleChanged { get; set; }

    [Parameter]
    public string Scope { get; set; }

    [Inject]
    public IFieldDefinitionManager FieldDefinitionManager { get; set; }

    public FieldScopeDefinition FieldScopeDefinition { get; set; }

    public RuleValidator RuleValidator { get; set; }

    protected override void OnParametersSet()
    {
        FieldScopeDefinition = FieldDefinitionManager.GetScope(Scope);
        RuleValidator = new RuleValidator(FieldScopeDefinition);

        if (MainRule != null)
        {
            MainRule.IsMainGroup = true;
        }
    }
}