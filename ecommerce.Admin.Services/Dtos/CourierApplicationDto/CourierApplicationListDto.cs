using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.CourierApplicationDto;

public class CourierApplicationListDto
{
    public int Id { get; set; }
    public int ApplicationUserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? IdentityNumber { get; set; }
    public string? Note { get; set; }
    public string? TaxNumber { get; set; }
    public string? TaxOffice { get; set; }
    public string? IBAN { get; set; }
    public int? CityId { get; set; }
    public int? TownId { get; set; }
    public string? CityName { get; set; }
    public string? TownName { get; set; }
    public string? TaxPlatePath { get; set; }
    public string? SignatureDeclarationPath { get; set; }
    public string? IdCopyPath { get; set; }
    public string? CriminalRecordPath { get; set; }
    public CourierApplicationStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime AppliedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int? ReviewedByUserId { get; set; }
    public string? RejectReason { get; set; }
}
