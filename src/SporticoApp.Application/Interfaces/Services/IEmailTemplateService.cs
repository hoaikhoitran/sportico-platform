namespace SporticoApp.Application.Interfaces.Services
{
    public interface IEmailTemplateService
    {
        string BuildVerifyEmailTemplate(string fullName, string verifyLink);

        string BuildResetPasswordTemplate(string fullName, string resetLink);
    }
}
