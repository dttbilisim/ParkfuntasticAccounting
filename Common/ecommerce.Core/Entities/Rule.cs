using ecommerce.Core.Rules;

namespace ecommerce.Core.Entities;

public class Rule
{
    public string Field { get; set; } = null!;

    public string? Value { get; set; }

    public RuleExpressionOperator Operator { get; set; }

    public RuleGroupOperator? GroupOperator { get; set; }

    public Rule? Parent { get; set; }

    public List<Rule>? Children { get; set; }
}