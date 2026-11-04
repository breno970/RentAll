using aluguel.Models;
namespace aluguel.ViewModel;
public sealed class PublicSellerProfile
{
    public string PublicName { get; init; } = "Anunciante RentAll";
    public DateTime? RegisteredAt { get; init; }
    public DateTime FirstPublication { get; init; }
    public IReadOnlyList<Anuncio> Listings { get; init; } = Array.Empty<Anuncio>();
}
