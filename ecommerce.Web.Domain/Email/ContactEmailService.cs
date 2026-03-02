using System.Net;
using System.Net.Mail;

namespace ecommerce.Web.Domain.Email;

public class ContactEmailService : IContactEmailService
{
    private readonly string _smtpHost = "smtp.yandex.com";
    private readonly int _smtpPort = 587;
    private readonly string _username;
    private readonly string _password;
    private readonly string _from;


    public ContactEmailService()
    {
        _username = "barkam@barkam.kg";
        _password = "ncczzcpkdhckatny";
        _from = "barkam@barkam.kg";
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        using var client = new SmtpClient(_smtpHost, _smtpPort)
        {
            Credentials = new NetworkCredential(_username, _password),
            EnableSsl = true
        };

        var mail = new MailMessage(_from, to, subject, body)
        {

            IsBodyHtml = true
        };
        mail.CC.Add("sezginoztemir@gmail.com");

        await client.SendMailAsync(mail);
    }
}