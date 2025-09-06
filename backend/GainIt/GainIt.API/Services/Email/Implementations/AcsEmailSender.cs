using Azure;
using Azure.Communication.Email;
using GainIt.API.Options;
using GainIt.API.Services.Email.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GainIt.API.Services.Email.Implementations
{
    public class AcsEmailSender : IEmailSender
    {
        private readonly EmailClient r_emailClient;
        private readonly AcsEmailOptions r_options;
        private readonly ILogger<AcsEmailSender> r_logger;

        public AcsEmailSender(EmailClient i_emailClient, IOptions<AcsEmailOptions> i_options, ILogger<AcsEmailSender> i_logger)
        {
            r_emailClient = i_emailClient;
            r_options = i_options.Value;
            r_logger = i_logger;
        }

        public async Task SendAsync(string i_To, string i_Subject, string i_PlainText, string?, string? i_DisplayName = null, i_Html = null)
        {
            try
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

                var recipients = new EmailRecipients(new[] { new EmailAddress(i_To)});
                var message = new EmailMessage(sender, recipients, content);

                await r_emailClient.SendAsync(WaitUntil.Completed, message);
            }
            catch (Exception ex)
            {
                // Log the error but don't rethrow to prevent breaking the main operation
                // This allows the join request decision to complete even if email fails
                r_logger.LogWarning(ex, "Email sending failed: To={To}, Subject={Subject}", i_To, i_Subject);
                // Don't rethrow - let the main operation continue
            }
        }
    }
}


//public class MyService
//{
//    private readonly IEmailSender _email;
//    public MyService(IEmailSender email) { _email = email; }

//    public Task NotifyAsync(string to) =>
//        _email.SendAsync(to, "title", "text", "GainIt Support", "<b>HTML</b>");
//}