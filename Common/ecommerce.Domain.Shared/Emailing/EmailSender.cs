using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using ecommerce.Core.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
namespace ecommerce.Domain.Shared.Emailing;
public class EmailSender : IEmailSender{
    private readonly IConfiguration _configuration;
    public EmailSender(IConfiguration configuration){_configuration = configuration;}
    public async Task SendEmailAsync(string subject, string body, string toAddress, string toName, string ? fromAddress = null, string ? fromName = null, string ? replyToAddress = null, string ? replyToName = null, IEnumerable<string> ? bcc = null, IEnumerable<string> ? cc = null, IList<NameValue<string ?, byte[]>> ? attachments = null, IDictionary<string, string> ? headers = null){
        var emailSettings = _configuration.GetSection("EmailSetting");
        fromAddress ??= emailSettings.GetValue<string>("SmtpEmail");
        fromName ??= emailSettings.GetValue<string>("SmtpTitle");
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(new MailboxAddress(toName, toAddress));
        if(!string.IsNullOrEmpty(replyToAddress)){
            message.ReplyTo.Add(new MailboxAddress(replyToName, replyToAddress));
        }
        if(bcc != null){
            foreach(var address in bcc.Where(bccValue => !string.IsNullOrWhiteSpace(bccValue))){
                message.Bcc.Add(new MailboxAddress("", address.Trim()));
            }
        }
        if(cc != null){
            foreach(var address in cc.Where(ccValue => !string.IsNullOrWhiteSpace(ccValue))){
                message.Cc.Add(new MailboxAddress("", address.Trim()));
            }
        }
        message.Subject = subject;
        if(headers != null){
            foreach(var header in headers){
                message.Headers.Add(header.Key, header.Value);
            }
        }
        var multipart = new Multipart("mixed"){new TextPart(TextFormat.Html){Text = body}};
        if(attachments?.Any() == true){
            foreach(var attachment in attachments){
                var fileName = string.IsNullOrEmpty(attachment.Name) ? "attachment" : attachment.Name;
                multipart.Add(new MimePart(MimeTypes.GetMimeType(fileName)){Content = new MimeContent(new MemoryStream(attachment.Value)), ContentDisposition = new ContentDisposition(ContentDisposition.Attachment), ContentTransferEncoding = ContentEncoding.Base64, FileName = fileName});
            }
        }
        message.Body = multipart;
        using var smtpClient = await BuildClientAsync(emailSettings);
        await smtpClient.SendAsync(message);
        await smtpClient.DisconnectAsync(true);
    }
    private async Task<SmtpClient> BuildClientAsync(IConfigurationSection emailSettings){
        var smtpClient = new SmtpClient(){ServerCertificateValidationCallback = ValidateServerCertificate};
        var host = emailSettings.GetValue<string>("SmtpHost");
        var port = emailSettings.GetValue<int>("SmtpPort");
        // Port 465 = implicit SSL (SslOnConnect). Port 587/25 = STARTTLS.
        var secureSocketOptions = port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
        try{
            await smtpClient.ConnectAsync(host, port, secureSocketOptions);
            var username = emailSettings.GetValue<string>("SmtpEmail");
            var password = emailSettings.GetValue<string>("SmtpPassword");
            if(!string.IsNullOrWhiteSpace(username)){
                await smtpClient.AuthenticateAsync(new NetworkCredential(username, password));
            }
            return smtpClient;
        } catch{
            smtpClient.Dispose();
            throw;
        }
    }
    private bool ValidateServerCertificate(object sender, X509Certificate ? certificate, X509Chain ? chain, SslPolicyErrors sslpolicyerrors){return true;}
}
