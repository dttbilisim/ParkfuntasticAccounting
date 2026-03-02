using AutoMapper;
using ecommerce.Admin.Domain.Dtos.RulesDto;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.PopupDto;

[AutoMap(typeof(Popup), ReverseMap = true)]
public class PopupUpsertDto
{
    public int? Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Title { get; set; }

    public string? Body { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int Order { get; set; }

    public PopupTrigger Trigger { get; set; }

    public string? TriggerReference { get; set; }

    public int TimeExpire { get; set; }

    public bool IsOnlyImage { get; set; }

    public string? Width { get; set; }

    public string? Height { get; set; }

    public int Status { get; set; }

    public RuleUpsertDto? Rule { get; set; }
}