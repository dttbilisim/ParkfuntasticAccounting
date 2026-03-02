using System.Linq.Expressions;
using ecommerce.Core.Entities;
using ecommerce.Core.Helpers;
using ecommerce.Core.Rules.Fields;
using ecommerce.Core.Utils.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NRules.RuleModel;
using NRules.RuleModel.Builders;

namespace ecommerce.Core.Rules;

public class RuleEngineRepository : IRuleEngineRepository
{
    private const string DefaultRuleSetName = "default";

    private IFieldDefinitionManager FieldDefinitionManager { get; }
    private ILogger<RuleEngineRepository> Logger { get; }
    private IServiceProvider ServiceProvider { get; }

    private List<IRuleSet> _ruleSets = new();

    private RuleEngineResult _result = new();

    public RuleEngineRepository(
        IFieldDefinitionManager fieldDefinitionManager,
        ILogger<RuleEngineRepository> logger,
        IServiceProvider serviceProvider)
    {
        FieldDefinitionManager = fieldDefinitionManager;
        Logger = logger;
        ServiceProvider = serviceProvider;
    }

    public IEnumerable<IRuleSet> GetRuleSets() => _ruleSets;

    public RuleEngineResult GetResult() => _result;

    public bool IsValid()
    {
        return _result.IsValid;
    }

    public async Task<IDisposable> BuildAsync(Rule rule, string scope)
    {
        var lockKey = nameof(RuleEngineRepository) + scope + GetHashCode();

        if (KeyedLock.IsLockHeld(lockKey))
        {
            return NullDisposable.Instance;
        }

        var keyedLock = await KeyedLock.LockAsync(lockKey);

        _result = new RuleEngineResult();

        var ruleset = new RuleSet(DefaultRuleSetName);
        _ruleSets = new List<IRuleSet> { ruleset };

        var builder = new RuleBuilder();
        builder.Name(scope);

        try
        {
            if (rule is not { GroupOperator: not null })
            {
                throw new Exception("Rule is not main rule");
            }

            var mainGroup = builder.LeftHandSide().Group(rule.GroupOperator == RuleGroupOperator.Or ? GroupType.Or : GroupType.And);

            await BuildRuleExpressions(scope, rule, mainGroup);

            BuildRuleAction(rule, builder.RightHandSide());

            ruleset.Add(builder.Build());
        }
        catch (Exception e)
        {
            Logger.LogError(e, e.Message);
        }

        return keyedLock;
    }

    private void BuildRuleAction(Rule rule, ActionGroupBuilder actionBuilder)
    {
        Expression<Action<IContext>> action = _ => _result.Modify(rule);

        actionBuilder.Action(action);
    }

    private async Task<bool> BuildRuleExpressions(string scope, Rule parent, GroupBuilder group)
    {
        var expressions = new List<Rule> { parent };

        if (parent.Children is { Count: > 0 })
        {
            expressions.AddRange(parent.Children);
        }

        var isApplicable = true;

        foreach (var expression in expressions)
        {
            if (expression != parent && expression.Children is { Count: > 0 })
            {
                var childGroup = group.Group(expression.GroupOperator == RuleGroupOperator.Or ? GroupType.Or : GroupType.And);

                if (!await BuildRuleExpressions(scope, expression, childGroup) && parent.GroupOperator == RuleGroupOperator.And)
                {
                    break;
                }

                continue;
            }

            var field = FieldDefinitionManager.GetField(scope, expression.Field);

            var providerValue = ServiceProvider.GetRequiredService(field.ValueProviderType) is IFieldDefinitionValueProvider valueProvider
                ? await valueProvider.GetAsync(field)
                : TypeHelper.GetDefaultValue(field.Type);

            var fallbackType = !TypeHelper.IsNullable(field.Type) && field.Type.IsValueType ? typeof(Nullable<>).MakeGenericType(field.Type) : field.Type;

            var value = Expression.Constant(providerValue, providerValue?.GetType() ?? fallbackType);

            var valuePattern = group.Pattern(value.Type);
            valuePattern.Binding().BindingExpression(Expression.Lambda(value));

            var valueParam = valuePattern.Declaration.ToParameterExpression();

            var condition = BuildCompareExpression(valueParam, expression, field);

            if (condition == null && parent.GroupOperator == RuleGroupOperator.And)
            {
                isApplicable = false;
                break;
            }

            valuePattern.Condition(condition);
        }

        return isApplicable;
    }

    private LambdaExpression? BuildCompareExpression(ParameterExpression valueParam, Rule expression, FieldDefinition field)
    {
        object? ruleValue = null;

        if (!RuleOperatorMapping.GetOperators(RuleExpressionOperatorType.Null).Contains(expression.Operator))
        {
            try
            {
                var strippedFieldType = TypeHelper.StripNullable(field.Type);

                if (new[] { typeof(DateTime), typeof(DateTimeOffset), typeof(TimeSpan), typeof(DateOnly), typeof(TimeOnly) }.Contains(strippedFieldType))
                {
                    var ruleDateTimeExpression = new RuleDateTimeExpression(expression.Value);

                    if (strippedFieldType == typeof(DateOnly))
                    {
                        ruleValue = DateOnly.FromDateTime(ruleDateTimeExpression.Date?.Date ?? DateTime.Now.Date.Add(ruleDateTimeExpression.ToTimeSpan()));
                    }
                    else if (strippedFieldType == typeof(TimeOnly))
                    {
                        ruleValue = TimeOnly.FromDateTime(ruleDateTimeExpression.Date ?? DateTime.Now.Add(ruleDateTimeExpression.ToTimeSpan()));
                    }
                    else if (strippedFieldType == typeof(TimeSpan))
                    {
                        ruleValue = TimeOnly.FromDateTime(ruleDateTimeExpression.Date ?? DateTime.Now.Add(ruleDateTimeExpression.ToTimeSpan())).ToTimeSpan();
                    }
                    else
                    {
                        ruleValue = ruleDateTimeExpression.Date ?? DateTime.Now.Add(ruleDateTimeExpression.ToTimeSpan());
                    }
                }
                else if (RuleOperatorMapping.GetOperators(RuleExpressionOperatorType.Array).Contains(expression.Operator))
                {
                    var isEnumerable = TypeHelper.IsEnumerable(strippedFieldType, out var fieldItemType, false);

                    ruleValue = expression.Value?.Split(RuleConsts.ExpressionValueArrayDelimiter)
                        .Select(v => FieldValueDictionary.ChangeType(v, isEnumerable ? fieldItemType! : strippedFieldType))
                        .ToList();
                }
                else
                {
                    ruleValue = FieldValueDictionary.ChangeType(expression.Value, strippedFieldType);
                }
            }
            catch
            {
                // ignored
            }

            if (ruleValue == null)
            {
                return null;
            }
        }

        var compareValue = Expression.Constant(ruleValue, ruleValue?.GetType() ?? field.Type);

        var compareExpression = GetCompareExpression(expression, field, valueParam, compareValue);

        if (compareExpression == null)
        {
            return null;
        }

        return Expression.Lambda(compareExpression, valueParam);
    }

    private Expression? GetCompareExpression(Rule expression, FieldDefinition field, Expression value, Expression compareValue)
    {
        var operators = RuleOperatorMapping.GetOperators(field.Type);

        if (!operators.Contains(expression.Operator))
        {
            return null;
        }

        var nullableValue = TypeHelper.IsNullable(value.Type) || !value.Type.IsValueType ? value : null;
        var nullableCompareValue = TypeHelper.IsNullable(compareValue.Type) || !compareValue.Type.IsValueType ? compareValue : null;

        if (TypeHelper.IsNullable(value.Type))
        {
            value = Expression.Coalesce(value, Expression.Constant(TypeHelper.GetDefaultValue(TypeHelper.StripNullable(value.Type))));
        }

        if (TypeHelper.IsNullable(compareValue.Type))
        {
            compareValue = Expression.Coalesce(compareValue, Expression.Constant(TypeHelper.GetDefaultValue(TypeHelper.StripNullable(compareValue.Type))));
        }

        var valueIsEnumerable = TypeHelper.IsEnumerable(TypeHelper.StripNullable(value.Type), out var valueUnwrappedType, false);

        Expression? condition = null;

        switch (expression.Operator)
        {
            case RuleExpressionOperator.Equal:
                condition = Expression.Equal(value, compareValue);
                break;
            case RuleExpressionOperator.NotEqual:
                condition = Expression.NotEqual(value, compareValue);
                break;
            case RuleExpressionOperator.GreaterThan:
                condition = Expression.GreaterThan(value, compareValue);
                break;
            case RuleExpressionOperator.GreaterThanOrEqual:
                condition = Expression.GreaterThanOrEqual(value, compareValue);
                break;
            case RuleExpressionOperator.LessThan:
                condition = Expression.LessThan(value, compareValue);
                break;
            case RuleExpressionOperator.LessThanOrEqual:
                condition = Expression.LessThanOrEqual(value, compareValue);
                break;
            case RuleExpressionOperator.StartsWith:
                condition = Expression.Call(typeof(RuleEngineExpressionHelper), nameof(RuleEngineExpressionHelper.StartsWith), null, value, compareValue);
                break;
            case RuleExpressionOperator.EndsWith:
                condition = Expression.Call(typeof(RuleEngineExpressionHelper), nameof(RuleEngineExpressionHelper.EndsWith), null, value, compareValue);
                break;
            case RuleExpressionOperator.Contains:
                condition = Expression.Call(typeof(RuleEngineExpressionHelper), nameof(RuleEngineExpressionHelper.Contains), null, value, compareValue);
                break;
            case RuleExpressionOperator.NotContains:
                condition = Expression.Call(typeof(RuleEngineExpressionHelper), nameof(RuleEngineExpressionHelper.NotContains), null, value, compareValue);
                break;
            case RuleExpressionOperator.In:
                condition = valueIsEnumerable
                    ? Expression.Call(typeof(RuleEngineExpressionHelper), nameof(RuleEngineExpressionHelper.ArrayIn), new[] { valueUnwrappedType! }, value, compareValue)
                    : Expression.Call(typeof(RuleEngineExpressionHelper), nameof(RuleEngineExpressionHelper.In), new[] { value.Type }, value, compareValue);
                break;
            case RuleExpressionOperator.NotIn:
                condition = valueIsEnumerable
                    ? Expression.Call(typeof(RuleEngineExpressionHelper), nameof(RuleEngineExpressionHelper.ArrayNotIn), new[] { valueUnwrappedType! }, value, compareValue)
                    : Expression.Call(typeof(RuleEngineExpressionHelper), nameof(RuleEngineExpressionHelper.NotIn), new[] { value.Type }, value, compareValue);
                break;
            case RuleExpressionOperator.IsNotNull:
                condition = Expression.NotEqual(nullableValue ?? value, Expression.Constant(null, (nullableValue ?? value).Type));
                break;
            case RuleExpressionOperator.IsNull:
                condition = Expression.Equal(nullableValue ?? value, Expression.Constant(null, (nullableValue ?? value).Type));
                break;
        }

        if (condition != null && !RuleOperatorMapping.GetOperators(RuleExpressionOperatorType.Null).Contains(expression.Operator))
        {
            BinaryExpression? valueNotNullExpression = null;
            BinaryExpression? compareValueNotNullExpression = null;

            if (nullableValue != null)
            {
                valueNotNullExpression = Expression.NotEqual(nullableValue, Expression.Constant(null, nullableValue.Type));
            }

            if (nullableCompareValue != null)
            {
                compareValueNotNullExpression = Expression.NotEqual(nullableCompareValue, Expression.Constant(null, nullableCompareValue.Type));
            }

            var notNullExpression = valueNotNullExpression != null && compareValueNotNullExpression != null
                ? Expression.And(valueNotNullExpression, compareValueNotNullExpression)
                : (valueNotNullExpression ?? compareValueNotNullExpression);

            if (notNullExpression != null)
            {
                condition = Expression.And(notNullExpression, condition);
            }
        }

        return condition;
    }
}