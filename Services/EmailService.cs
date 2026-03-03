using MailKit.Net.Smtp;
using MimeKit;

namespace HotelBuffetPass.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                _config["Email:SenderName"] ?? "Hotel Events",
                _config["Email:SenderEmail"] ?? "events@hotel.com"
            ));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(
                _config["Email:SmtpHost"],
                int.Parse(_config["Email:SmtpPort"] ?? "587"),
                MailKit.Security.SecureSocketOptions.StartTls
            );
            await client.AuthenticateAsync(
                _config["Email:Username"],
                _config["Email:Password"]
            );
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
