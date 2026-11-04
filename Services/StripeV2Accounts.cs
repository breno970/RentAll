using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace aluguel.Services;

/// <summary>Accounts v2 recipient onboarding. Only sandbox keys, no personal documents in RentAll.</summary>
public sealed class StripeV2Accounts
{
    private readonly HttpClient _http;
    private readonly StripePaymentOptions _options;
    public StripeV2Accounts(HttpClient http, IOptions<StripePaymentOptions> options)
    {
        _http = http; _options = options.Value;
        _http.BaseAddress = new Uri("https://api.stripe.com/");
        _http.Timeout = TimeSpan.FromSeconds(20);
    }

    private async Task<JsonDocument> RequestAsync(HttpMethod method, string path, object? data, string? key, CancellationToken ct)
    {
        if (!_options.IsTestReady) throw new InvalidOperationException("Stripe sandbox desativada.");
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.SecretKey);
        request.Headers.TryAddWithoutValidation("Stripe-Version", "2026-10-28.preview");
        if (key != null) request.Headers.TryAddWithoutValidation("Idempotency-Key", key);
        if (data != null) request.Content = JsonContent.Create(data);
        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("A Stripe não aceitou a operação de conta de teste (HTTP " + (int)response.StatusCode + ").");
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
    }

    public async Task<string> CreateRecipientAsync(string userId, string email, CancellationToken ct)
    {
        var body = new {
            contact_email = email,
            display_name = "Anunciante RentAll",
            identity = new { country = "pt" },
            dashboard = "express",
            defaults = new { responsibilities = new { fees_collector = "application", losses_collector = "application" } },
            configuration = new {
                recipient = new {
                    capabilities = new { stripe_balance = new { stripe_transfers = new { requested = true } } }
                }
            },
            include = new[] { "configuration.recipient", "requirements" }
        };
        using var response = await RequestAsync(HttpMethod.Post, "v2/core/accounts", body,
            "rentall-sandbox-seller-" + userId, ct);
        return response.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Conta Stripe sem ID.");
    }

    public async Task<string> OnboardingUrlAsync(string accountId, string baseUrl, CancellationToken ct)
    {
        var body = new {
            account = accountId,
            use_case = new {
                type = "account_onboarding",
                account_onboarding = new {
                    configurations = new[] { "recipient" },
                    refresh_url = baseUrl + "/Pagamentos/Reautorizar",
                    return_url = baseUrl + "/Pagamentos/Conta"
                }
            }
        };
        using var response = await RequestAsync(HttpMethod.Post, "v2/core/account_links", body, null, ct);
        return response.RootElement.GetProperty("url").GetString()
            ?? throw new InvalidOperationException("Link Stripe indisponível.");
    }

    public async Task<bool> CanReceiveAsync(string accountId, CancellationToken ct)
    {
        using var response = await RequestAsync(HttpMethod.Get,
            "v2/core/accounts/" + Uri.EscapeDataString(accountId) + "?include%5B%5D=configuration.recipient",
            null, null, ct);
        return response.RootElement.GetProperty("configuration").GetProperty("recipient")
            .GetProperty("capabilities").GetProperty("stripe_balance")
            .GetProperty("stripe_transfers").GetProperty("status").GetString() == "active";
    }
}
