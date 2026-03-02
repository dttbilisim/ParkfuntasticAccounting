namespace OtoIsmail.Dtos;
public class LoginResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int Rowcount { get; set; }
    public double Duration { get; set; }
}