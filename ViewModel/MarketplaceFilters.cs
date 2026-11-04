namespace aluguel.ViewModel;

/// <summary>Filtros da vitrine RentAll. Todos são opcionais e preservados na paginação.</summary>
public sealed class MarketplaceFilters
{
    public string Pais { get; set; } = "BR";
    public string? SearchString { get; set; }
    public string? Estado { get; set; }
    /// <summary>Filtro da cidade; mantém o nome da query antiga para não quebrar URLs.</summary>
    public string? RegionFilter { get; set; }
    public string? Bairro { get; set; }
    public int? CategoriaId { get; set; }
    public float? MinPrice { get; set; }
    public float? MaxPrice { get; set; }
    public string Sort { get; set; } = "recentes";
    public int? Page { get; set; }

    public void Normalize()
    {
        Pais = Pais?.ToUpperInvariant() == "PT" ? "PT" : "BR";
        SearchString = NormalizeText(SearchString);
        Estado = NormalizeText(Estado);
        if (Pais == "BR") Estado = Estado?.ToUpperInvariant();
        RegionFilter = NormalizeText(RegionFilter);
        Bairro = NormalizeText(Bairro);
        if (CategoriaId <= 0) CategoriaId = null;
        if (MinPrice is < 0 or > 10000000 || !float.IsFinite(MinPrice ?? 0)) MinPrice = null;
        if (MaxPrice is < 0 or > 10000000 || !float.IsFinite(MaxPrice ?? 0)) MaxPrice = null;
        if (MinPrice.HasValue && MaxPrice.HasValue && MinPrice > MaxPrice)
            (MinPrice, MaxPrice) = (MaxPrice, MinPrice);
        if (Page is < 1 or > 10000) Page = 1;
        Sort = Sort is "preco-menor" or "preco-maior" or "recentes" ? Sort : "recentes";
        if (!aluguel.Services.LocationCatalog.IsValidRegion(Pais, Estado)) Estado = null;
    }

    private static string? NormalizeText(string? text) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Trim()[..Math.Min(text.Trim().Length, 90)];
}
