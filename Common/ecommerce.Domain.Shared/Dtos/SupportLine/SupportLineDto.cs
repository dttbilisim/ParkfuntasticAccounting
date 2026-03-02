using AutoMapper;
using ecommerce.Core.Utils;

namespace ecommerce.Domain.Shared.Dtos.SupportLine;

public class SupportLineDto
{
    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public int FrequentlyAskedQuestionsId{get;set;}

    public string Description { get; set; } = null!;

    public SupportLinereturnType? SupportLineReturnType { get; set; }

    public SupportLineType? SupportLineType { get; set; }

    public string? Token { get; set; }
}