using ecommerce.Core.Entities;
using NRules.RuleModel;

namespace ecommerce.Core.Rules;

public interface IRuleEngineRepository : IRuleRepository
{
    Task<IDisposable> BuildAsync(Rule rule, string scope);

    RuleEngineResult GetResult();

    bool IsValid();
}