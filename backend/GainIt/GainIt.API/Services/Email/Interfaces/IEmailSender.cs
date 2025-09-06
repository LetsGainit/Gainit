namespace GainIt.API.Services.Email.Interfaces
{
    public interface IEmailSender
    {
        Task SendAsync(string i_To, string i_Subject, string i_PlainText, string? i_DisplayName = null, string? i_Html = null);
    }
}
