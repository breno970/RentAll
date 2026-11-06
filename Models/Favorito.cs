using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace aluguel.Models;

public sealed class Favorito
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = "";

    [Required]
    public int AnuncioId { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(AnuncioId))]
    public Anuncio? Anuncio { get; set; }
}
