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
    public decimal TotalEstimado { get; set; }
    // Indicative platform business model only. No payment is processed or collected.
    public decimal ComissaoIndicativa { get; set; }
    [MaxLength(30)] public string Estado { get; set; } = "Pendente";
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? RespondidoEm { get; set; }
}
