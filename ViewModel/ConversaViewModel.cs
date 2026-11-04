using aluguel.Models;
namespace aluguel.ViewModel;

public sealed class ConversaViewModel
{
    public int AnuncioId { get; init; }
    public string NomeArtigo { get; init; } = "";
    public string OutroUtilizadorId { get; init; } = "";
    public string MeuId { get; init; } = "";
    public bool SouAnunciante { get; init; }
    public bool AnuncioAtivo { get; init; }
    public float PrecoDia { get; init; }
    public int PrazoMaximo { get; init; } = 365;
    public string Pais { get; init; } = "PT";
    public int? PedidoId { get; init; }
    public string? EstadoPedido { get; init; }
    public int? DiasPedido { get; init; }
    public decimal? TotalPedido { get; init; }
    public IReadOnlyList<Mensagem> Mensagens { get; init; } = Array.Empty<Mensagem>();
}
public sealed class ResumoConversa
{
    public int AnuncioId { get; init; }
    public string OutroUtilizadorId { get; init; } = "";
    public string NomeArtigo { get; init; } = "";
    public DateTime UltimaMensagem { get; init; }
    public bool SouAnunciante { get; init; }
}
