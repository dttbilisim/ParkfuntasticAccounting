using ecommerce.Core.Models;

namespace ecommerce.Domain.Shared.Emailing;

public interface IEmailSender
{
    Task SendEmailAsync(
        string subject,
        string body,
        string toAddress,
        string toName,
        string? fromAddress = null,
        string? fromName = null,
        string? replyToAddress = null,
        string? replyToName = null,
        IEnumerable<string>? bcc = null,
        IEnumerable<string>? cc = null,
        IList<NameValue<string?, byte[]>>? attachments = null,
        IDictionary<string, string>? headers = null);
}