using aluguel.Areas.Identity.Data;
using aluguel.Models;
using aluguel.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace aluguel.Controllers;

[ApiController]
public sealed class StripeWebhooksController : ControllerBase
{
    private readonly ApplicationDbContext db;
    private readonly StripeTestGateway stripe;
    private readonly ILogger<StripeWebhooksController> log;
    public StripeWebhooksController(ApplicationDbContext db,StripeTestGateway stripe,
        ILogger<StripeWebhooksController> log) { this.db=db;this.stripe=stripe;this.log=log; }

    [HttpPost("/api/stripe/webhook")]
    [RequestSizeLimit(262144)]
    public async Task<IActionResult> Handle(CancellationToken ct)
    {
        if (!stripe.Available) return StatusCode(503);
        using var reader = new StreamReader(Request.Body);
        string payload = await reader.ReadToEndAsync(ct);
        Event evt;
        try { evt = stripe.VerifiedEvent(payload,Request.Headers["Stripe-Signature"].ToString()); }
        catch (Exception ex) when (ex is StripeException or ArgumentException or InvalidOperationException)
        { return BadRequest("Assinatura Stripe inválida."); }
        if (string.IsNullOrWhiteSpace(evt.Id) || evt.Livemode) return BadRequest("Apenas eventos da sandbox são aceites.");
        await using var txn = await db.Database.BeginTransactionAsync(ct);
        if (await db.EventosStripeProcessados.AnyAsync(x=>x.EventId==evt.Id,ct)) return Ok();

        if (evt.Data.Object is Session session && (evt.Type=="checkout.session.completed" ||
            evt.Type=="checkout.session.async_payment_succeeded" ||
            evt.Type=="checkout.session.async_payment_failed" ||
            evt.Type=="checkout.session.expired"))
        {
            var payment=await db.PagamentosAluguer.SingleOrDefaultAsync(p=>p.StripeSessionId==session.Id,ct);
            if (payment == null) return StatusCode(503);
            if (session.ClientReferenceId != payment.PedidoId.ToString() ||
                session.AmountTotal != payment.TotalCentimos ||
                !string.Equals(session.Currency,payment.Moeda,StringComparison.OrdinalIgnoreCase))
            {
                log.LogError("Stripe session mismatch in payment ledger ID {Id}",payment.Id);
                return BadRequest("Sessão ou montantes inválidos.");
            }
            if (session.PaymentStatus=="paid" &&
                (evt.Type=="checkout.session.completed" || evt.Type=="checkout.session.async_payment_succeeded"))
            {
                if (payment.Estado == "Checkout criado" || payment.Estado == "Falhado" || payment.Estado == "Expirado")
                    payment.Estado="Pago em teste";
                payment.PagoEm ??=DateTime.UtcNow;
                payment.StripePaymentIntentId=session.PaymentIntentId;
            }
            else if (payment.Estado!="Pago em teste")
            {
                if(evt.Type=="checkout.session.expired") payment.Estado="Expirado";
                if(evt.Type=="checkout.session.async_payment_failed") payment.Estado="Falhado";
            }
            payment.AtualizadoEm=DateTime.UtcNow;
        }
        // Reconcile test refunds using the original PaymentIntent reference.
        // Charge.amount_refunded is cumulative, so retries and partial refunds remain idempotent.
        if (evt.Type == "charge.refunded" && evt.Data.Object is Charge charge &&
            !string.IsNullOrEmpty(charge.PaymentIntentId))
        {
            var payment = await db.PagamentosAluguer.FirstOrDefaultAsync(
                p => p.StripePaymentIntentId == charge.PaymentIntentId, ct);
            if (payment == null) return StatusCode(503);
            if (charge.Amount < payment.TotalCentimos ||
                charge.AmountRefunded < 0 || charge.AmountRefunded > payment.TotalCentimos)
                return BadRequest("Valor de reembolso incompatível.");
            payment.ReembolsadoCentimos = Math.Max(payment.ReembolsadoCentimos, charge.AmountRefunded);
            if (payment.ReembolsadoCentimos == payment.TotalCentimos)
                payment.Estado = "Reembolsado em teste";
            else if (payment.ReembolsadoCentimos > 0 && !payment.DisputaAberta)
                payment.Estado = "Reembolso parcial";
            payment.AtualizadoEm = DateTime.UtcNow;
        }

        // A dispute is not a completed refund. Keep it visible to staff for manual review.
        if ((evt.Type == "charge.dispute.created" || evt.Type == "charge.dispute.closed") &&
            evt.Data.Object is Dispute dispute &&
            !string.IsNullOrEmpty(dispute.PaymentIntentId))
        {
            var payment = await db.PagamentosAluguer.FirstOrDefaultAsync(
                p => p.StripePaymentIntentId == dispute.PaymentIntentId, ct);
            if (payment == null) return StatusCode(503);
            if (evt.Type == "charge.dispute.created")
            {
                payment.DisputaAberta = true;
                if (payment.Estado != "Reembolsado em teste") payment.Estado = "Disputa aberta";
            }
            else
            {
                payment.DisputaAberta = false;
                if (payment.Estado != "Reembolsado em teste") payment.Estado = "Disputa encerrada";
            }
            payment.AtualizadoEm = DateTime.UtcNow;
        }

        // Record other signed events for reconciliation; don't infer refunds or successful
        // payment from an unknown event without verifying its object.
        db.EventosStripeProcessados.Add(new EventoStripeProcessado {
            EventId=evt.Id, Tipo=evt.Type, ProcessadoEm=DateTime.UtcNow
        });
        try
        {
            await db.SaveChangesAsync(ct);
            await txn.CommitAsync(ct);
            return Ok();
        }
        catch(DbUpdateException ex)
        {
            log.LogWarning(ex,"Stripe webhook persistence failed; provider may retry");
            return StatusCode(503);
        }
    }
}
