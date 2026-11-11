using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace aluguel.Services;

/// <summary>
/// Validates contact details and attempts to divert rental negotiations to external
/// channels. Applied on the server to new messages, adverts and public profiles.
/// This is a preventive text check, not a user-risk score or account suspension.
/// </summary>
public static partial class MarketplaceContactSafety
{
    public enum Reason
    {
        None,
        Phone,
        Email,
        Link,
        Social,
        ExternalPayment
    }

    private const RegexOptions SafeOptions =
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

    [GeneratedRegex(@"(?<![\p{L}\p{N}])(?:\+|00)?(?:\d[\s.()\-_/|,]*?){9,15}(?!\d)",
        SafeOptions, 250)]
    private static partial Regex Phone();

    [GeneratedRegex(@"[\p{L}\d_.+\-]{2,}\s*(?:@|\(\s*at\s*\)|\[\s*at\s*\]|arroba)\s*[\p{L}\d.\-]{2,}\s*(?:\.|\(\s*ponto\s*\)|\[\s*ponto\s*\]|\s+ponto\s+)\s*(?:com|pt|br|net|org|io|eu)\b",
        SafeOptions, 250)]
    private static partial Regex Email();

    [GeneratedRegex(@"(?:https?\s*:\s*/\s*/|www\s*\.|\b(?:wa|t)\s*\.\s*me\b|\bdiscord\s*\.\s*gg\b|\blinktr\s*\.\s*ee\b|\bbit\s*\.\s*ly\b|\b[a-z0-9][a-z0-9-]{1,40}\s*(?:\.|\[\s*ponto\s*\]|\(\s*ponto\s*\)|\s+ponto\s+)\s*(?:com|pt|br|net|org|io|me|co|eu|app|ly)\b)",
        SafeOptions, 250)]
    private static partial Regex Link();

    [GeneratedRegex(@"(?:\B@[a-z][a-z0-9._]{2,30}\b|\b(?:whats\s*app|whats|wpp|zap\s*zap|zapzap|telegram|insta\s*gram|instagram|facebook|messenger|signal|snapchat|discord|tik\s*tok|tiktok)\b)",
        SafeOptions, 250)]
    private static partial Regex Social();

    [GeneratedRegex(@"\b(?:mb\s*way|pix|paypal|revolut|iban|transferencia\s+bancaria|pagar\s+(?:por|via|fora\s+da)\s+\w+)\b",
        SafeOptions, 250)]
    private static partial Regex ExternalPayment();

    [GeneratedRegex(@"\b(?:zero|um|uma|dois|duas|tres|quatro|cinco|seis|sete|oito|nove)\b",
        SafeOptions, 250)]
    private static partial Regex SpokenDigit();

    private static readonly Dictionary<string, string> Digits = new(StringComparer.Ordinal)
    {
        ["zero"] = "0", ["um"] = "1", ["uma"] = "1", ["dois"] = "2",
        ["duas"] = "2", ["tres"] = "3", ["quatro"] = "4",
        ["cinco"] = "5", ["seis"] = "6", ["sete"] = "7",
        ["oito"] = "8", ["nove"] = "9"
    };

    private static string Normalize(string source)
    {
        // Compatibility folding handles full-width digits and symbols. Remove invisible
        // formatting characters so "9\u200b1\u200b2" is not a bypass.
        var decomposed = source.Normalize(NormalizationForm.FormKC)
            .Normalize(NormalizationForm.FormD);
        var buffer = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category is UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark
                or UnicodeCategory.Format or UnicodeCategory.Control) continue;
            buffer.Append(char.ToLowerInvariant(ch));
        }
        return buffer.ToString().Normalize(NormalizationForm.FormC);
    }

    public static Reason Classify(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Reason.None;
        // Input validation also limits the maximum text length in individual controllers.
        var text = Normalize(value.Length > 8000 ? value[..8000] : value);
        var spoken = SpokenDigit().Replace(text, m => Digits[m.Value]);
        if (Phone().IsMatch(text) || Phone().IsMatch(spoken)) return Reason.Phone;
        if (Email().IsMatch(text)) return Reason.Email;
        if (Link().IsMatch(text)) return Reason.Link;
        if (Social().IsMatch(text)) return Reason.Social;
        if (ExternalPayment().IsMatch(text)) return Reason.ExternalPayment;
        return Reason.None;
    }

    public static bool ContainsContactOrRedirect(string? text) => Classify(text) != Reason.None;

    public static string UserMessage(Reason reason) => reason switch
    {
        Reason.Phone => "Não é permitido enviar números de telemóvel. Combina o aluguer nas mensagens da RentAll.",
        Reason.Email => "Retira o endereço de e-mail. A negociação deve permanecer na RentAll.",
        Reason.Link => "Não envies links externos. Utiliza o anúncio, as mensagens e os pedidos da RentAll.",
        Reason.Social => "Não partilhes perfis ou contactos de outras aplicações. Continua a conversa na RentAll.",
        Reason.ExternalPayment => "Não solicites pagamentos fora da RentAll. Os pagamentos reais ainda não estão disponíveis.",
        _ => ""
    };
}
