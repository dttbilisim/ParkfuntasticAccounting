using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Rules;
using ecommerce.Core.Rules.Fields;

namespace ecommerce.Admin.Domain.Dtos.RulesDto;

public class RuleUpsertDto
{
    public string? Field { get; set; }

    public string? Value { get; set; }

    public RuleExpressionOperator? Operator { get; set; }

    public RuleGroupOperator? GroupOperator { get; set; }

    public List<RuleUpsertDto> Children { get; set; } = new();

    [NotMapped]
    public List<string>? Values { get; set; }

    [NotMapped]
    public bool IsGroup { get; set; }

    [NotMapped]
    public bool IsMainGroup { get; set; }

    [NotMapped]
    public bool IsGroupOperatorEnabled { get; set; }

    [NotMapped]
    public FieldDefinition? FieldDefinition { get; set; }
}