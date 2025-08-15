namespace GainIt.API.Services.Email.Interfaces
{
    public interface IEmailSender
    {
        Task SendAsync(string to, string subject, string plainText, string? html = null, string? displayName = null);
    }
}
