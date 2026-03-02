using System.ComponentModel.DataAnnotations;
namespace ecommerce.Core.Entities;
public class BankParameter{
    [Key]
    public int Id { get; set; }
    public BankParameter()
    {
    }

    public BankParameter(string key, string value)
    {
        Key = key;
        Value = value;
    }

    public int BankId { get; set; }
    /// <summary>Şube bazlı (Banka Parametreleri şirket/şubeye göre ayrılır).</summary>
    public int? BranchId { get; set; }
    public string Key { get; set; }
    public string Value { get; set; }

    public Bank Bank { get; set; }
}
