namespace aluguel.ViewModel;

public sealed class CartaoDemoViewModel
{
    public string Artigo { get; init; } = "Aluguer de artigo";
    public decimal Total { get; init; } = 50m;
    public decimal Comissao { get; init; } = 5m;
    public string Pais { get; init; } = "PT";
    public string? Foto { get; init; }
}
