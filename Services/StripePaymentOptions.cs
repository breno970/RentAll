namespace aluguel.Services;

public sealed class StripePaymentOptions
{
    public bool Enabled { get; set; }
    public string PlatformCountry { get; set; } = "PT";
    public string SecretKey { get; set; } = "";
    public string WebhookSecret { get; set; } = "";
    public bool IsTestReady => Enabled && SecretKey.StartsWith("sk_test_", StringComparison.Ordinal)
        && !string.IsNullOrWhiteSpace(WebhookSecret) && PlatformCountry == "PT";
}
