using AutoMapper;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.SupportLineDto;

[AutoMap(typeof(SupportLine))]
public class SupportLineUpsertDto
{
    public int Id { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string? FrequentlyAskedQuestionName { get; set; }
    public int FrequentlyAskedQuestionsId { get; set; }

    public string Description { get; set; } = null!;

    public SupportLinereturnType? SupportLineReturnType { get; set; }

    public SupportLineType SupportLineType { get; set; }

    public int Status { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? Note { get; set; }
}