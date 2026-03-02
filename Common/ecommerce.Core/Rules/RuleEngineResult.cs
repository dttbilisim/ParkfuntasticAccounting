using ecommerce.Core.Entities;

namespace ecommerce.Core.Rules;

public class RuleEngineResult
{
    public Rule? AppliedRule { get; private set; }

    public bool IsValid => AppliedRule != null;

    public void Modify(Rule rule)
    {
        AppliedRule = rule;
    }
}