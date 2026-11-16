using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace FlowerShop.Services.Email
{
    public interface IEmailService
    {
        /// <summary>Tên provider để hiển thị ở trang admin/diagnostics.</summary>
        string ProviderName { get; }

        /// <summary>Gửi mail; trả về true nếu đã bàn giao cho provider thành công.</summary>
        bool Send(string toEmail, string subject, string body, out string error);
    }

    /// <summary>
    /// Gửi mail qua SMTP. Cấu hình trong Web.config (mục appSettings):
    /// Smtp:Host, Smtp:Port, Smtp:User, Smtp:Password, Smtp:From, Smtp:FromName, Smtp:EnableSsl.
    /// Với Laragon, trỏ Host=localhost Port=1025 (MailHog) là đủ để test.
    /// </summary>
    public class SmtpEmailService : IEmailService
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _user;
        private readonly string _password;
        private readonly string _from;
        private readonly string _fromName;
        private readonly bool _enableSsl;

        public string ProviderName
        {
            get { return "SMTP"; }
        }

        public SmtpEmailService(string host, int port, string user, string password, string from,
            string fromName, bool enableSsl)
        {
            _host = host;
            _port = port;
            _user = user;
            _password = password;
            _from = from;
            _fromName = fromName;
            _enableSsl = enableSsl;
        }

        public static SmtpEmailService FromConfiguration()
        {
            return new SmtpEmailService(
                ConfigurationManager.AppSettings["Smtp:Host"] ?? "localhost",
                ParseInt(ConfigurationManager.AppSettings["Smtp:Port"], 1025),
                ConfigurationManager.AppSettings["Smtp:User"],
                ConfigurationManager.AppSettings["Smtp:Password"],
                ConfigurationManager.AppSettings["Smtp:From"] ?? "no-reply@huuphuocflorist.vn",
                ConfigurationManager.AppSettings["Smtp:FromName"] ?? "Hữu Phước Florist",
                ParseBool(ConfigurationManager.AppSettings["Smtp:EnableSsl"], false));
        }

        public bool Send(string toEmail, string subject, string body, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(toEmail))
            {
                error = "Địa chỉ email người nhận rỗng.";
                return false;
            }

            try
            {
                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(_from, _fromName, Encoding.UTF8);
                    message.To.Add(new MailAddress(toEmail.Trim()));
                    message.Subject = subject;
                    message.SubjectEncoding = Encoding.UTF8;
                    message.Body = body;
                    message.BodyEncoding = Encoding.UTF8;
                    message.IsBodyHtml = true;

                    using (var client = new SmtpClient(_host, _port))
                    {
                        client.EnableSsl = _enableSsl;
                        client.DeliveryMethod = SmtpDeliveryMethod.Network;

                        if (!string.IsNullOrEmpty(_user))
                        {
                            client.UseDefaultCredentials = false;
                            client.Credentials = new NetworkCredential(_user, _password);
                        }

                        client.Timeout = 15000;
                        client.Send(message);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static int ParseInt(string value, int fallback)
        {
            int parsed;
            return int.TryParse(value, out parsed) ? parsed : fallback;
        }

        private static bool ParseBool(string value, bool fallback)
        {
            bool parsed;
            return bool.TryParse(value, out parsed) ? parsed : fallback;
        }
    }

    /// <summary>
    /// Fallback cho môi trường dev không có SMTP (Laragon chưa bật MailHog):
    /// ghi mail ra file .eml trong thư mục chỉ định, không làm hỏng luồng nghiệp vụ
    /// và vẫn cho phép kiểm thử tự động.
    /// </summary>
    public class FileEmailService : IEmailService
    {
        private readonly string _directory;

        public string ProviderName
        {
            get { return "FILE"; }
        }

        public FileEmailService(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("Thư mục lưu mail không hợp lệ.", "directory");
            }

            _directory = directory;
        }

        public static FileEmailService FromConfiguration(string mapPathRoot)
        {
            string configured = ConfigurationManager.AppSettings["Smtp:FileFallbackDirectory"];
            string dir = !string.IsNullOrWhiteSpace(configured)
                ? configured
                : Path.Combine(mapPathRoot ?? AppDomain.CurrentDomain.BaseDirectory, "App_Data", "emails");

            return new FileEmailService(dir);
        }

        public bool Send(string toEmail, string subject, string body, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(toEmail) || toEmail.IndexOf('@') <= 0)
            {
                error = "Địa chỉ email người nhận không hợp lệ.";
                return false;
            }

            try
            {
                if (!Directory.Exists(_directory))
                {
                    Directory.CreateDirectory(_directory);
                }

                string fileName = string.Format("{0:yyyyMMdd_HHmmss_fff}_{1}.eml",
                    DateTime.Now,
                    Guid.NewGuid().ToString("N").Substring(0, 8));

                string content = string.Format(
                    "To: {0}{1}Subject: {2}{1}Date: {3:O}{1}Content-Type: text/html; charset=utf-8{1}{1}{4}",
                    toEmail, Environment.NewLine, subject, DateTime.Now, body);

                File.WriteAllText(Path.Combine(_directory, fileName), content, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        /// <summary>Số mail đã ghi - dùng cho diagnostics và test.</summary>
        public int GetSentCount()
        {
            if (!Directory.Exists(_directory))
            {
                return 0;
            }

            return Directory.GetFiles(_directory, "*.eml").Length;
        }
    }

    public static class EmailServiceFactory
    {
        /// <summary>
        /// Chọn provider: nếu Smtp:Host được cấu hình thì dùng SMTP, ngược lại ghi file.
        /// </summary>
        public static IEmailService Create(string mapPathRoot)
        {
            string host = ConfigurationManager.AppSettings["Smtp:Host"];

            if (!string.IsNullOrWhiteSpace(host))
            {
                return SmtpEmailService.FromConfiguration();
            }

            return FileEmailService.FromConfiguration(mapPathRoot);
        }
    }
}
