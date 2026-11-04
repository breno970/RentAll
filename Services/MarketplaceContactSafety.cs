using System.Text.RegularExpressions;

namespace aluguel.Services;

/// <summary>
/// Light-touch protection against publishing contact details or diverting rental
/// negotiations to other apps. Not a guarantee that circumvention is impossible.
/// </summary>
public static partial class MarketplaceContactSafety
{
    [GeneratedRegex(@"(?:https?://|www\.|(?:[a-z0-9-]+\.)+(?:com|pt|br|net|org|io|me)\b|[\w.+-]+@[\w.-]+\.[A-Za-z]{2,})",RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ExternalAddress();

    [GeneratedRegex(@"(?<!\d)(?:\+?\d[\s.()\-]?){9,15}(?!\d)",RegexOptions.CultureInvariant)]
    private static partial Regex PhoneNumber();

    [GeneratedRegex(@"\b(?:whats\s?app|wpp|telegram|t\.me|instagram|insta\s?gram|facebook|messenger|signal|snapchat|discord)\b",RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ExternalChannel();

    [GeneratedRegex(@"(?<!\w)@[a-z][a-z0-9._]{2,29}\b",RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SocialHandle();

    [GeneratedRegex(@"\b(?:manda(?:-me)?\s+(?:mensagem|msg)\s+(?:no|pelo)\s+\w+|paga(?:r)?\s+(?:por|via)\s+(?:mb\s?way|pix)|faz(?:er)?\s+transfer[eê]ncia\s+(?:antes|primeiro))\b",RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OffPlatformPayment();

    public static bool ContainsContactOrRedirect(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var normalized = string.Concat(text.Normalize(System.Text.NormalizationForm.FormKC)
            .Where(c => !char.IsControl(c) || c == '\n' || c == '\r'));
        return ExternalAddress().IsMatch(normalized) ||
            PhoneNumber().IsMatch(normalized) ||
            SocialHandle().IsMatch(normalized) ||
            ExternalChannel().IsMatch(normalized) ||
            OffPlatformPayment().IsMatch(normalized);
    }
}
