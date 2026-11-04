using System.ComponentModel.DataAnnotations;

namespace aluguel.Models;

public sealed class ContaStripeLocador
{
    [Key] public string UserId { get; set; } = "";
    [Required, MaxLength(128)] public string AccountId { get; set; } = "";
    [MaxLength(2)] public string Pais { get; set; } = "PT";
    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
}

public sealed class PagamentoAluguer
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    [Required, MaxLength(128)] public string CompradorId { get; set; } = "";
    [Required, MaxLength(128)] public string LocadorId { get; set; } = "";
    [Required, MaxLength(128)] public string StripeAccountId { get; set; } = "";
    [Required, MaxLength(128)] public string StripeSessionId { get; set; } = "";
    [MaxLength(128)] public string? StripePaymentIntentId { get; set; }
    [MaxLength(3)] public string Moeda { get; set; } = "eur";
    public long TotalCentimos { get; set; }
    public long ComissaoCentimos { get; set; }
    [MaxLength(24)] public string Estado { get; set; } = "Checkout criado";
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? PagoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
}

public sealed class EventoStripeProcessado
{
    [Key, MaxLength(128)] public string EventId { get; set; } = "";
    [MaxLength(100)] public string Tipo { get; set; } = "";
    public DateTime ProcessadoEm { get; set; } = DateTime.UtcNow;
}
