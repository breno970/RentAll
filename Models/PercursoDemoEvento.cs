using System.ComponentModel.DataAnnotations;

namespace aluguel.Models;

/// <summary>
/// Append-only audit trail of a demo rental journey: who confirmed what and when,
/// condition checklists, accessories, notes and return discrepancies.
/// Never a financial record.
/// </summary>
public sealed class PercursoDemoEvento
{
    public long Id { get; set; }
    public int PedidoId { get; set; }
    [MaxLength(450)] public string AutorId { get; set; } = "";
    [MaxLength(16)] public string Papel { get; set; } = "";
    [MaxLength(32)] public string Operacao { get; set; } = "";
    [MaxLength(32)] public string? EstadoArtigo { get; set; }
    [MaxLength(300)] public string? Acessorios { get; set; }
    [MaxLength(500)] public string? Nota { get; set; }
    [MaxLength(120)] public string? FotoKey { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
