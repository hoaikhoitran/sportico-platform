using System.Net;
using SporticoApp.Application.Interfaces.Services;

namespace SporticoApp.Infrastructure.Services
{
    /// <summary>
    /// Builds professional, mobile-friendly transactional email bodies using inline CSS only.
    /// No external assets, no tracking pixels.
    /// </summary>
    public class EmailTemplateService : IEmailTemplateService
    {
        private const string BrandName = "Sportico";
        private const string PrimaryColor = "#1f7a4d";
        private const string TextColor = "#1f2933";
        private const string MutedColor = "#6b7280";
        private const string BackgroundColor = "#f4f5f7";

        public string BuildVerifyEmailTemplate(string fullName, string verifyLink)
        {
            var greetingName = WebUtility.HtmlEncode(ResolveGreetingName(fullName));
            var safeLink = WebUtility.HtmlEncode(verifyLink);
            var button = BuildButton("Verify email", safeLink);
            var fallback = BuildFallback("verify your account", safeLink);

            var content = $@"
                <p style=""margin:0 0 16px 0;font-size:16px;line-height:24px;color:{TextColor};"">
                    Hi {greetingName},
                </p>
                <p style=""margin:0 0 24px 0;font-size:16px;line-height:24px;color:{TextColor};"">
                    Welcome to {BrandName}! Please confirm your email address to activate your account
                    and start exploring coaches and training packages.
                </p>
                {button}
                {fallback}
                <p style=""margin:24px 0 0 0;font-size:13px;line-height:20px;color:{MutedColor};"">
                    If you did not create a {BrandName} account, you can safely ignore this email.
                </p>";

            return BuildLayout("Verify your email", content);
        }

        public string BuildResetPasswordTemplate(string fullName, string resetLink)
        {
            var greetingName = WebUtility.HtmlEncode(ResolveGreetingName(fullName));
            var safeLink = WebUtility.HtmlEncode(resetLink);
            var button = BuildButton("Reset password", safeLink);
            var fallback = BuildFallback("reset your password", safeLink);

            var content = $@"
                <p style=""margin:0 0 16px 0;font-size:16px;line-height:24px;color:{TextColor};"">
                    Hi {greetingName},
                </p>
                <p style=""margin:0 0 24px 0;font-size:16px;line-height:24px;color:{TextColor};"">
                    We received a request to reset the password for your {BrandName} account.
                    Click the button below to choose a new password.
                </p>
                {button}
                {fallback}
                <p style=""margin:24px 0 0 0;font-size:13px;line-height:20px;color:{MutedColor};"">
                    This link will expire shortly for your security. If you did not request a password
                    reset, you can safely ignore this email and your password will remain unchanged.
                </p>";

            return BuildLayout("Reset your password", content);
        }

        private static string ResolveGreetingName(string? fullName)
        {
            return string.IsNullOrWhiteSpace(fullName) ? "there" : fullName.Trim();
        }

        private static string BuildButton(string label, string safeLink)
        {
            return $@"
                <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""margin:0 0 8px 0;"">
                    <tr>
                        <td style=""border-radius:6px;background-color:{PrimaryColor};"">
                            <a href=""{safeLink}""
                               style=""display:inline-block;padding:12px 28px;font-size:16px;font-weight:600;
                                      color:#ffffff;text-decoration:none;border-radius:6px;"">
                                {label}
                            </a>
                        </td>
                    </tr>
                </table>";
        }

        private static string BuildFallback(string action, string safeLink)
        {
            return $@"
                <p style=""margin:16px 0 0 0;font-size:13px;line-height:20px;color:{MutedColor};"">
                    If the button does not work, copy and paste this link into your browser to {action}:
                </p>
                <p style=""margin:4px 0 0 0;font-size:13px;line-height:20px;word-break:break-all;"">
                    <a href=""{safeLink}"" style=""color:{PrimaryColor};"">{safeLink}</a>
                </p>";
        }

        private static string BuildLayout(string title, string content)
        {
            return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>{title}</title>
</head>
<body style=""margin:0;padding:0;background-color:{BackgroundColor};
             font-family:Segoe UI,Roboto,Helvetica,Arial,sans-serif;"">
    <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0""
           style=""background-color:{BackgroundColor};padding:24px 0;"">
        <tr>
            <td align=""center"">
                <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0""
                       style=""max-width:560px;background-color:#ffffff;border-radius:10px;
                              overflow:hidden;border:1px solid #e5e7eb;"">
                    <tr>
                        <td style=""background-color:{PrimaryColor};padding:20px 32px;"">
                            <span style=""font-size:22px;font-weight:700;color:#ffffff;
                                         letter-spacing:0.5px;"">{BrandName}</span>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding:32px;"">
                            {content}
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding:20px 32px;border-top:1px solid #e5e7eb;"">
                            <p style=""margin:0;font-size:12px;line-height:18px;color:{MutedColor};"">
                                &copy; {System.DateTime.UtcNow.Year} {BrandName}. All rights reserved.
                            </p>
                            <p style=""margin:6px 0 0 0;font-size:12px;line-height:18px;color:{MutedColor};"">
                                This is an automated message, please do not reply.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }
    }
}
