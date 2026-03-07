namespace ecommerce.Core.Entities;

/// <summary>PcPos uyumluluğu: Uzak uygulama sürüm bilgisi</summary>
public class VersionApp
{
    public int Id { get; set; }
    public string PcPosVersion { get; set; } = string.Empty;
}
