using System.ComponentModel.DataAnnotations;

namespace aluguel.Models;

public sealed class PedidoAluguer
{
    public int Id { get; set; }
    public int AnuncioId { get; set; }
    [Required] public string InteressadoId { get; set; } = "";
    [Required] public string AnuncianteId { get; set; } = "";
    [Range(1, 365)] public int Dias { get; set; }
    // Snapshot of the agreed daily asking price when the request was created.
    public decimal PrecoDia { get; set; }
    [MaxLength(2)] public string Pais { get; set; } = "PT";
    public decimal TotalEstimado { get; set; }
    // Indicative platform business model only. No payment is processed or collected.
    public decimal ComissaoIndicativa { get; set; }
    // Pending counter-offer. Original rental terms remain unchanged until the other party accepts.
    public int? PropostaDias { get; set; }
    public decimal? PropostaPrecoDia { get; set; }
    public string? PropostaPorId { get; set; }
    public DateTime? PropostaEm { get; set; }
    [MaxLength(30)] public string Estado { get; set; } = "Pendente";
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? RespondidoEm { get; set; }
}
