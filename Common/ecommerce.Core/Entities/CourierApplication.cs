using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Utils;

namespace ecommerce.Core.Entities;

/// <summary>
/// Mobil kullanıcının "Kuryem Olur musun" başvurusu. Onaylanınca Courier kaydı oluşturulur.
/// </summary>
public class CourierApplication
{
    public int Id { get; set; }

    [ForeignKey(nameof(ApplicationUserId))]
    public ApplicationUser? ApplicationUser { get; set; }
    public int ApplicationUserId { get; set; }

    public CourierApplicationStatus Status { get; set; } = CourierApplicationStatus.Pending;

    public DateTime AppliedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int? ReviewedByUserId { get; set; }
    public string? RejectReason { get; set; }

    public string Phone { get; set; } = string.Empty;
    public string? IdentityNumber { get; set; }
    public string? Note { get; set; }

    // Vergi Levhası, İmza Beyannamesi, Kimlik Fotokopisi, Sabıka Kaydı — dosya yolu (Uploads/CourierDocuments/...)
    public string? TaxPlatePath { get; set; }
    public string? SignatureDeclarationPath { get; set; }
    public string? IdCopyPath { get; set; }
    public string? CriminalRecordPath { get; set; }

    // Vergi Numarası, Vergi Dairesi, IBAN
    public string? TaxNumber { get; set; }
    public string? TaxOffice { get; set; }
    public string? IBAN { get; set; }

    public int? CityId { get; set; }
    public int? TownId { get; set; }

    [ForeignKey(nameof(CityId))]
    public City? City { get; set; }
    [ForeignKey(nameof(TownId))]
    public Town? Town { get; set; }
}
