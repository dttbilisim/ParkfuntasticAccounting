using ecommerce.Core.Attributes;
using ecommerce.Core.Helpers;
using ecommerce.Core.Rules;
using ecommerce.Core.Rules.Fields;
using FluentValidation;
using FluentValidation.Results;

#pragma warning disable CS0618

namespace ecommerce.Admin.Domain.Dtos.RulesDto;

[DisableFluentValidatorRegistration]
public class RuleValidator : AbstractValidator<RuleUpsertDto>
{
    private FieldScopeDefinition FieldScopeDefinition { get; }

    public RuleValidator(FieldScopeDefinition fieldScopeDefinition)
    {
        FieldScopeDefinition = fieldScopeDefinition;

        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(m => m.Field)
            .Must(
                (m, v, ctx) =>
                {
                    if (v == null && m.IsMainGroup && !m.Children.Any())
                    {
                        return true;
                    }

                    var field = ctx.RootContextData.TryGetValue("FieldDefinition", out var fd) ? (FieldDefinition) fd : null;

                    return field != null;
                }
            );

        RuleFor(m => m.Value)
            .NotNull()
            .Must(
                (m, v, ctx) =>
                {
                    var field = ctx.RootContextData.TryGetValue("FieldDefinition", out var fd) ? (FieldDefinition) fd : null;

                    if (field == null) return false;

                    try
                    {
                        if (new[] { typeof(DateTime), typeof(DateTimeOffset), typeof(TimeSpan), typeof(DateOnly), typeof(TimeOnly) }.Contains(TypeHelper.StripNullable(field.Type)))
                        {
                            return RuleDateTimeExpression.TryParse(v, out _);
                        }

                        FieldValueDictionary.ChangeType(v, field.Type);
                    }
                    catch
                    {
                        return false;
                    }

                    return true;
                }
            )
            .When(
                f => f.Operator != null && !RuleOperatorMapping.GetOperators(RuleExpressionOperatorType.Null)
                    .Concat(RuleOperatorMapping.GetOperators(RuleExpressionOperatorType.Array))
                    .Contains(f.Operator.Value)
            );

        RuleFor(m => m.Values)
            .NotEmpty()
            .Must(
                (_, v, ctx) =>
                {
                    if (v == null)
                    {
                        return true;
                    }

                    var field = ctx.RootContextData.TryGetValue("FieldDefinition", out var fd) ? (FieldDefinition) fd : null;

                    if (field == null) return false;

                    if (v.Count > RuleConsts.ExpressionValueArrayCount)
                    {
                        return false;
                    }

                    try
                    {
                        if (TypeHelper.IsEnumerable(field.Type, out var fieldItemType, false))
                        {
                            foreach (var value in v)
                            {
                                FieldValueDictionary.ChangeType(value, fieldItemType);
                            }
                        }
                        else
                        {
                            foreach (var value in v)
                            {
                                FieldValueDictionary.ChangeType(value, field.Type);
                            }
                        }
                    }
                    catch
                    {
                        return false;
                    }

                    return true;
                }
            )
            .When(f => f.Operator != null && RuleOperatorMapping.GetOperators(RuleExpressionOperatorType.Array).Contains(f.Operator.Value));

        RuleFor(m => m.Operator)
            .NotNull()
            .IsInEnum()
            .Must(
                (_, v, ctx) =>
                {
                    var field = ctx.RootContextData.TryGetValue("FieldDefinition", out var fd) ? (FieldDefinition) fd : null;

                    if (field == null || v == null) return false;

                    var operators = RuleOperatorMapping.GetOperators(field.Type);

                    return operators.Contains(v.Value);
                }
            );

        RuleFor(m => m.GroupOperator).NotNull().IsInEnum().When(d => d.IsGroup);

        RuleForEach(m => m.Children).SetValidator(this);
    }

    protected override bool PreValidate(ValidationContext<RuleUpsertDto> context, ValidationResult result)
    {
        var model = context.InstanceToValidate;
        var field = model?.Field != null ? FieldScopeDefinition.GetFieldOrNull(model.Field) : null;

        context.RootContextData["FieldDefinition"] = field;

        return base.PreValidate(context, result);
    }
}