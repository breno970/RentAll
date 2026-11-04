using System.ComponentModel.DataAnnotations;

namespace aluguel.Models;

/// <summary>Private moderation report. Never publicly displayed on a listing or profile.</summary>
public sealed class DenunciaAnuncio
{
    public int Id { get; set; }
    public int AnuncioId { get; set; }
    [Required] public string AutorId { get; set; } = "";
    [Required, MaxLength(50)] public string Motivo { get; set; } = "";
    [MaxLength(700)] public string? Detalhes { get; set; }
    [MaxLength(24)] public string Estado { get; set; } = "Pendente";
    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
}
