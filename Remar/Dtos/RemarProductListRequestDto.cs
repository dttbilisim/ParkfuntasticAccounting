namespace Remar.Dtos;
public class RemarProductListRequestDto{
    public string SearchText { get; set; } = "";
    public string ProductCode { get; set; } = "";
    public int PageNo { get; set; } = 1;
    public int PageLen { get; set; } = 5000;
}
