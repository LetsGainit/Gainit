using Azure;
using Azure.Communication.Email;
using GainIt.API.Options;
using GainIt.API.Services.Email.Interfaces;
using Microsoft.Extensions.Options;

namespace GainIt.API.Services.Email.Implementations
{
    public class AcsEmailSender : IEmailSender
    {
        private readonly EmailClient r_emailClient;

        private readonly AcsEmailOptions r_options;

        public AcsEmailSender(EmailClient i_emailClient, IOptions<AcsEmailOptions> i_options)
        {
            r_emailClient = i_emailClient;
            r_options = i_options.Value;
        }

        public async Task SendAsync(string i_To, string i_Subject, string i_PlainText, string? i_Html = null, string? i_DisplayName = null)
        {
            var sender = string.IsNullOrWhiteSpace(i_DisplayName)
                ? r_options.Sender
                : $"{i_DisplayName} <{r_options.Sender}>";

            var content = new EmailContent(i_Subject)
            {
                PlainText = i_PlainText
            };
            if (!string.IsNullOrWhiteSpace(i_Html))
                content.Html = i_Html;

            var recipients = new EmailRecipients(new[] { new EmailAddress(i_To) });
            var message = new EmailMessage(sender, recipients, content);

            await r_emailClient.SendAsync(WaitUntil.Completed, message);
        }
    }
}


//public class MyService
//{
//    private readonly IEmailSender _email;
//    public MyService(IEmailSender email) { _email = email; }

//    public Task NotifyAsync(string to) =>
//        _email.SendAsync(to, "title", "text", "<b>HTML</b>", "GainIt Support");
//}