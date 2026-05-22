using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Infrastructure.Persistence.Configurations
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;

        public int Port { get; set; }

        public string SenderName { get; set; } = string.Empty;

        public string SenderEmail { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}
