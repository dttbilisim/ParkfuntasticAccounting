using ecommerce.Core.Models;
using ecommerce.Core.Utils;
namespace ecommerce.Domain.Shared.Emailing;

[Serializable]
public class EmailSendJobArgs
{
    public EmailTemplateType TemplateName{
        get;
        set;
    }

    public string Subject { get; set; } = null!;

    public EmailTokenDictionary? Tokens { get; set; }

    public string ToAddress { get; set; } = null!;

    public string ToName { get; set; } = null!;

    public string? FromAddress { get; set; }

    public string? FromName { get; set; }

    public string? ReplyToAddress { get; set; }

    public string? ReplyToName { get; set; }

    public IEnumerable<string>? Bcc { get; set; }

    public IEnumerable<string>? Cc { get; set; }

    public IList<NameValue<string?, byte[]>>? Attachments { get; set; }

    public IDictionary<string, string>? Headers { get; set; }
}