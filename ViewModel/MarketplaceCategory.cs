namespace aluguel.ViewModel;

public sealed record MarketplaceCategory(int Id, string Action, string Name, string Photo)
{
    public static IReadOnlyList<MarketplaceCategory> All { get; } = Array.AsReadOnly(new MarketplaceCategory[]
    {
        new(5, "Ferramentas", "Ferramentas", "ferramentas.jpg"),
        new(2, "AudioVisual", "Áudio e vídeo", "camera.jpg"),
        new(4, "EsporteGinastica", "Desporto e lazer", "bicicleta.jpg"),
        new(1, "VideoGame", "Consolas e jogos", "consolas.jpg"),
        new(3, "MusicaHobbie", "Música e hobbies", "guitarra.jpg"),
        new(6, "Eletrodomesticos", "Casa e cozinha", "cafe.jpg")
    });

    public static IEnumerable<string> ListingPhotos(aluguel.Models.Item? item)
    {
        if (item is null) return Enumerable.Empty<string>();

        // Older demo listings use the site logo as their stored cover.
        var isDemo = item.nmitem.StartsWith("[DEMO] ", StringComparison.Ordinal);
        return new[] { item.imagem1, item.imagem2, item.imagem3, item.imagem4, item.imagem5 }
            .OfType<string>()
            .Where(photo => !string.IsNullOrWhiteSpace(photo) && !(isDemo && photo == "ra.png"));
    }

    public static string PhotoFor(int? categoryId) =>
        All.FirstOrDefault(category => category.Id == categoryId)?.Photo ?? "aluguer.jpg";

    public static string IllustrationFor(aluguel.Models.Item? item, int? categoryId) =>
        DemoCatalogItem.ForListing(item?.nmitem)?.Photo ?? PhotoFor(categoryId);
}
