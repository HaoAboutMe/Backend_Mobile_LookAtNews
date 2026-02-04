using LookAt_Server.Config;
using Microsoft.Extensions.Options;
using System.Net.Mail;

namespace LookAt_Server.Services
{
    public class EmailService
    {
        private readonly MailSettings _settings;

        // Constructor khởi tạo EmailService với cấu hình MailSettings
        public EmailService(IOptions<MailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendEmailAsync(String toEmail, String subject, String body)
        {
            var mail = new MailMessage()
            {
                From = new MailAddress(_settings.Mail, _settings.DisplayName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mail.To.Add(new MailAddress(toEmail));

            // Sử dụng SmtpClient để gửi email
            using var smtp = new SmtpClient(_settings.Host, _settings.Port)
            {
                //Credentials là thông tin xác thực để đăng nhập vào máy chủ SMTP
                Credentials = new System.Net.NetworkCredential(_settings.Mail, _settings.Password),
                EnableSsl = true
            };

            await smtp.SendMailAsync(mail);
        }
    }
}
