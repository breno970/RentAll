using aluguel.Models;

namespace aluguel.ViewModel;

public sealed class PercursoDemoViewModel
{
    public PedidoAluguer Pedido { get; init; } = null!;
    public PercursoAluguerDemo? Percurso { get; init; }
    public string Artigo { get; init; } = "";
    public bool SouAnunciante { get; init; }
    public bool HistoricoDisponivel { get; init; }
    public IReadOnlyList<PercursoDemoEvento> Eventos { get; init; } = Array.Empty<PercursoDemoEvento>();
}
