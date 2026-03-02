using Nest;

namespace ecommerce.Domain.Shared.Dtos.Banners;

public class BannerItemDto
{
    [PropertyName("Id")]
    public int Id { get; set; }

    [PropertyName("BannerId")]
    public int BannerId { get; set; }

    [PropertyName("BannerType")]
    public int BannerType { get; set; }

    [PropertyName("Status")]
    public int Status { get; set; }

    [PropertyName("Order")]
    public int Order { get; set; }

    [PropertyName("FileGuid")]
    public string FileGuid { get; set; } = null!;

    [PropertyName("FileName")]
    public string FileName { get; set; } = null!;

    [PropertyName("Root")]
    public string Root { get; set; } = null!;

    [PropertyName("Title")]
    public string Title { get; set; } = null!;

    [PropertyName("Description")]
    public string? Description { get; set; }

    [PropertyName("Url")]
    public string Url { get; set; } = null!;

    [PropertyName("IsButton")]
    public bool IsButton { get; set; }

    [PropertyName("ButtonName")]
    public string? ButtonName { get; set; }

    [PropertyName("IsNewTab")]
    public bool IsNewTab { get; set; }

    [PropertyName("BannerCount")]
    public int BannerCount { get; set; }

    [PropertyName("MobileImageUrl")]
    public string? MobileImageUrl { get; set; }

    [PropertyName("FileNameMobile")]
    public string? FileNameMobile { get; set; }

    [PropertyName("FullSlider")]
    public bool FullSlider { get; set; }

    [PropertyName("StartDate")]
    public DateTime? StartDate { get; set; }

    [PropertyName("EndDate")]
    public DateTime? EndDate { get; set; }

    [PropertyName("IsVideo")]
    public bool IsVideo { get; set; }

    [PropertyName("VideoUrl")]
    public string? VideoUrl { get; set; }
}
