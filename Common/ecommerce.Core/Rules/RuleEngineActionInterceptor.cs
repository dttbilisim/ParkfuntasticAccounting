using Microsoft.Extensions.Logging;
using NRules.Extensibility;
using NRules.RuleModel;

namespace ecommerce.Core.Rules;

public class RuleEngineActionInterceptor : IActionInterceptor
{
    private readonly ILogger<RuleEngineActionInterceptor> _logger;

    public RuleEngineActionInterceptor(ILogger<RuleEngineActionInterceptor> logger)
    {
        _logger = logger;
    }

    public void Intercept(IContext context, IEnumerable<IActionInvocation> actions)
    {
        try
        {
            foreach (var action in actions)
            {
                action.Invoke();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Rule execution failed. Name={name}", context.Rule.Name);
        }
    }
}