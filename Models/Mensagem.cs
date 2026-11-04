using System.ComponentModel.DataAnnotations;

namespace aluguel.Models;
public sealed class Mensagem
{
    [Key] public int Id { get; set; }
    public int AnuncioId { get; set; }
    [Required] public string RemetenteId { get; set; } = "";
    [Required] public string DestinatarioId { get; set; } = "";
    [Required, StringLength(2000)] public string Texto { get; set; } = "";
    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
}
