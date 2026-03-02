using System.ComponentModel.DataAnnotations;
namespace ecommerce.Core.Entities;
public class BankCard{
    [Key]
    public int Id { get; set; }
    public int BankId { get; set; }
    /// <summary>Şube bazlı (Banka Kartları şirket/şubeye göre ayrılır).</summary>
    public int? BranchId { get; set; }
    public string Name { get; set; }
    public bool Active { get; set; }
    public bool ManufacturerCard { get; set; }
    public bool CampaignCard { get; set; }
    public bool Deleted { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime UpdateDate { get; set; }

    public Bank Bank { get; set; }
    public List<BankCreditCardPrefix> Prefixes { get; set; } = new List<BankCreditCardPrefix>();
    public List<BankCreditCardInstallment> Installments { get; set; } = new List<BankCreditCardInstallment>();
}
