using aluguel.Models;
namespace aluguel.ViewModel;

public sealed class ConversaViewModel
{
    public int AnuncioId { get; init; }
    public string NomeArtigo { get; init; } = "";
    public string OutroUtilizadorId { get; init; } = "";
    public string MeuId { get; init; } = "";
    public bool SouAnunciante { get; init; }
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
