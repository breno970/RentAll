using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace aluguel.Services;

public sealed class EmailSender : IEmailSender
{
    private readonly ILogger<EmailSender> _logger;
    private readonly AuthMessageSenderOptions _options;

    public EmailSender(IOptions<AuthMessageSenderOptions> options, ILogger<EmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        if (string.IsNullOrWhiteSpace(_options.SendGridKey))
            throw new InvalidOperationException("O serviço de e-mail ainda não está configurado.");

        var client = new SendGridClient(_options.SendGridKey);
        var mail = new SendGridMessage
        {
            From = new EmailAddress("rent.all.bn@gmail.com", "Equipa RentAll"),
            Subject = subject,
            PlainTextContent = System.Text.RegularExpressions.Regex.Replace(message, "<[^>]+>", " "),
            HtmlContent = message
        };
        mail.AddTo(new EmailAddress(toEmail));
        mail.SetClickTracking(false, false);
        var response = await client.SendEmailAsync(mail);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Email provider returned HTTP {Code}.", (int)response.StatusCode);
            throw new InvalidOperationException("Não foi possível enviar o e-mail.");
        }
        _logger.LogInformation("Email notification successfully queued.");
    }
}
