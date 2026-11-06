using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net;
using System.Text.RegularExpressions;

namespace aluguel.Services;

/// <summary>Sends account-security email via a verified sender; rejects missing configuration.</summary>
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
        if (string.IsNullOrWhiteSpace(_options.SendGridKey) ||
            string.IsNullOrWhiteSpace(_options.EmailFromAddress))
            throw new InvalidOperationException("O envio de emails requer SendGridKey e EmailFromAddress configurados no servidor.");

        var email = new SendGridMessage
        {
            From = new EmailAddress(_options.EmailFromAddress.Trim(), _options.EmailFromName),
            Subject = subject,
            HtmlContent = EmailTemplate.Wrap(subject, message),
            PlainTextContent = EmailTemplate.ToPlainText(message)
        };
        email.AddTo(new EmailAddress(toEmail.Trim()));
        email.SetClickTracking(false, false);
        email.SetOpenTracking(false);
        try
        {
            var response = await new SendGridClient(_options.SendGridKey).SendEmailAsync(email);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("SendGrid rejected transactional message: HTTP {Status}. Check sender verification, API permissions and account status.", (int)response.StatusCode);
                throw new InvalidOperationException("O serviço de envio recusou o email. Verifica o domínio/remetente no SendGrid.");
            }
            _logger.LogInformation("Transactional email accepted by provider (HTTP {Status}).", (int)response.StatusCode);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Email provider request failed.");
            throw new InvalidOperationException("Falha temporária no envio do email.", ex);
        }
    }
}

internal static class EmailTemplate
{
    public static string Wrap(string subject, string body)
    {
        var safeSubject = WebUtility.HtmlEncode(subject);
        // Body is built from server-authored templates with encoded links, never user content.
        return $"""
        <!doctype html><html lang="pt"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
        <body style="margin:0;background:#f6f8f6;color:#27392e;font-family:Arial,Helvetica,sans-serif">
        <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#f6f8f6;padding:32px 12px"><tr><td align="center">
        <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:560px;background:#fff;border:1px solid #dde7df;border-radius:12px">
        <tr><td style="background:#173c32;color:white;font-size:23px;font-weight:bold;padding:25px 30px">RentAll</td></tr>
        <tr><td style="padding:28px 30px"><h1 style="font-size:21px;margin:0 0 16px">{safeSubject}</h1>
        <div style="font-size:15px;line-height:1.7">{body}</div>
        <p style="font-size:13px;color:#617368;margin-top:28px">Se não fizeste este pedido, podes ignorar esta mensagem. Nunca partilhes códigos ou palavras-passe.</p>
        </td></tr>
        <tr><td style="background:#f7f9f7;border-top:1px solid #dde7df;padding:18px 30px;font-size:12px;color:#617368">RentAll · Portugal e Brasil</td></tr>
        </table></td></tr></table></body></html>
        """;
    }

    public static string ToPlainText(string html)
        => WebUtility.HtmlDecode(Regex.Replace(html.Replace("</p>", "\n").Replace("<br>", "\n"), "<[^>]+>", " ")).Trim();
}
