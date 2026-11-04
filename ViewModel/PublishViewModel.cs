using Microsoft.AspNetCore.Http;

namespace aluguel.ViewModel;

/// <summary>Dados submetidos numa única publicação; nunca solicita coordenadas, morada ou telefone.</summary>
public sealed class PublishViewModel
{
    public string? Nome { get; set; }
    public string? Descricao { get; set; }
    public string Visibilidade { get; set; } = "S";
    public List<IFormFile> Photos { get; set; } = new();
    public string? PhotoOrder { get; set; }
    public int Categoria { get; set; }
    public int Tipo { get; set; }
    public string? Modelo { get; set; }
    public string? Preco { get; set; }
    public int? DiasAluguel { get; set; }
    public string Pais { get; set; } = "PT";
    public string? Estado { get; set; }
    public string? Cidade { get; set; }
    public string? Bairro { get; set; }
    public string? NomePublico { get; set; }
}
