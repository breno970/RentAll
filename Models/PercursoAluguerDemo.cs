using System.ComponentModel.DataAnnotations;

namespace aluguel.Models;

/// <summary>
/// Isolated, persistent rehearsal of a rental journey. Never a real payment ledger.
/// </summary>
public sealed class PercursoAluguerDemo
{
    [Key] public int PedidoId { get; set; }
    [MaxLength(32)] public string Estado { get; set; } = "Checkout";
    [MaxLength(16)] public string? MetodoPagamento { get; set; }
    public DateTime? PagamentoEm { get; set; }
    [MaxLength(32)] public string? EntregaTipo { get; set; }
    [MaxLength(120)] public string? EntregaLocal { get; set; }
    public DateTime? EntregaEm { get; set; }
    public DateTime? AcordadaEm { get; set; }
    public DateTime? EntreguePeloAnuncianteEm { get; set; }
    public DateTime? RecebidaPeloLocatarioEm { get; set; }
    public DateTime? DevolvidaPeloLocatarioEm { get; set; }
    public DateTime? RecebidaPeloAnuncianteEm { get; set; }
    public bool IncidenteAberto { get; set; }
    public int? Avaliacao { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
    public long Versao { get; set; }
}
