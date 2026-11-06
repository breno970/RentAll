namespace aluguel.Services;

public sealed class StripePaymentOptions
{
    public bool Enabled { get; set; }
    public string PlatformCountry { get; set; } = "PT";
    public string SecretKey { get; set; } = "";
    public string WebhookSecret { get; set; } = "";
    public string PublicBaseUrl { get; set; } = "";
    // Tax activation requires verified registrations and legal tax-liability decision.
    public bool TaxEnabled { get; set; } = false;
    // Fail closed if tax is requested before registration/liability integration is finished.
    public bool IsTestReady => Enabled && !TaxEnabled &&
        WebhookSecret.StartsWith("whsec_", StringComparison.Ordinal) &&
        (SecretKey.StartsWith("rk_test_", StringComparison.Ordinal) ||
        SecretKey.StartsWith("sk_test_", StringComparison.Ordinal))
        && !string.IsNullOrWhiteSpace(WebhookSecret) && PlatformCountry == "PT";
}
