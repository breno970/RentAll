using Stripe;
using Stripe.Checkout;

namespace aluguel.Services;

/// <summary>Stripe Connect integration: test mode only, disabled by default.</summary>
public sealed class StripeTestGateway
{
    private readonly StripePaymentOptions _settings;
    public StripeTestGateway(Microsoft.Extensions.Options.IOptions<StripePaymentOptions> config) =>
        _settings = config.Value;

    public bool Available => _settings.IsTestReady;
    public bool Supports(string country) => Available && country == "PT";

    private StripeClient Client()
    {
        if (!Available) throw new InvalidOperationException("Integração de pagamentos de teste indisponível.");
        return new StripeClient(_settings.SecretKey);
    }

    public async Task<Account> CreateAccountAsync(string ownerId, CancellationToken ct)
    {
        return await new AccountService(Client()).CreateAsync(
            new AccountCreateOptions { Type = "express", Country = "PT" },
            new RequestOptions { IdempotencyKey = "rentall-connect-" + ownerId }, ct);
    }

    public async Task<Account> GetAccountAsync(string id, CancellationToken ct) =>
        await new AccountService(Client()).GetAsync(id, cancellationToken: ct);

    public async Task<AccountLink> OnboardingAsync(string id, string origin, CancellationToken ct) =>
        await new AccountLinkService(Client()).CreateAsync(new AccountLinkCreateOptions
        {
            Account = id, Type = "account_onboarding",
            RefreshUrl = origin + "/Pagamentos/Conta",
            ReturnUrl = origin + "/Pagamentos/Conta"
        }, cancellationToken: ct);

    public async Task<Session> CreateCheckoutAsync(int requestId, long amountCents, long feeCents,
        string sellerAccount, string itemName, string origin, CancellationToken ct)
    {
        var options = new SessionCreateOptions
        {
            Mode = "payment",
            ClientReferenceId = requestId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            SuccessUrl = origin + "/Pedidos?resultado=checkout",
            CancelUrl = origin + "/Pedidos?resultado=cancelado",
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new() {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions {
                        Currency = "eur", UnitAmount = amountCents,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        { Name = itemName.Length > 110 ? itemName[..110] : itemName }
                    }
                }
            },
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                ApplicationFeeAmount = feeCents,
                TransferData = new SessionPaymentIntentDataTransferDataOptions { Destination = sellerAccount },
                Metadata = new Dictionary<string,string> { ["rentall_request_id"] = requestId.ToString() }
            },
            Metadata = new Dictionary<string,string> { ["rentall_request_id"] = requestId.ToString() }
        };
        return await new SessionService(Client()).CreateAsync(options,
            new RequestOptions { IdempotencyKey = "rentall-payment-" + requestId }, ct);
    }

    public Stripe.Event VerifiedEvent(string payload, string signature)
    {
        if (!Available) throw new InvalidOperationException("Webhook não configurado.");
        return EventUtility.ConstructEvent(payload, signature, _settings.WebhookSecret,
            throwOnApiVersionMismatch: false);
    }
}
