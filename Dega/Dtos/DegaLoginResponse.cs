namespace Dega.Dtos;
public class DegaLoginResponse
{
    public string d { get; set; }
    public Message message { get; set; }
    public string status { get; set; }
}

public class Message
{
    public List<string> Items { get; set; }
}
