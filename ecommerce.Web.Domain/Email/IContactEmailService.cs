namespace ecommerce.Web.Domain.Email;

public interface IContactEmailService
{
    Task SendEmailAsync(string to, string subject, string body);

}