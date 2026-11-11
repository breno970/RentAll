namespace aluguel.ViewModel;

public sealed class CartaoDemoViewModel
{
    public string Artigo { get; init; } = "Aluguer de artigo";
    public int PrecoDiaCentimos { get; init; } = 1800;
    public string Cidade { get; init; } = "Lisboa";
    public string? Foto { get; init; }
}
