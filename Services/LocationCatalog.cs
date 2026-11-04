namespace aluguel.Services;

/// <summary>Regiões administrativas dos dois mercados onde a RentAll opera.</summary>
public static class LocationCatalog
{
    public const string Brazil = "BR";
    public const string Portugal = "PT";

    public static readonly (string Code, string Name)[] States = new[]
    {
        ("AC","Acre"), ("AL","Alagoas"), ("AP","Amapá"), ("AM","Amazonas"),
        ("BA","Bahia"), ("CE","Ceará"), ("DF","Distrito Federal"), ("ES","Espírito Santo"),
        ("GO","Goiás"), ("MA","Maranhão"), ("MT","Mato Grosso"), ("MS","Mato Grosso do Sul"),
        ("MG","Minas Gerais"), ("PA","Pará"), ("PB","Paraíba"), ("PR","Paraná"),
        ("PE","Pernambuco"), ("PI","Piauí"), ("RJ","Rio de Janeiro"), ("RN","Rio Grande do Norte"),
        ("RS","Rio Grande do Sul"), ("RO","Rondônia"), ("RR","Roraima"), ("SC","Santa Catarina"),
        ("SP","São Paulo"), ("SE","Sergipe"), ("TO","Tocantins")
    };

    // Os distritos do continente e as duas regiões autónomas.
    public static readonly (string Code, string Name)[] PortugalRegions = new[]
    {
        ("Aveiro","Aveiro"), ("Beja","Beja"), ("Braga","Braga"), ("Bragança","Bragança"),
        ("Castelo Branco","Castelo Branco"), ("Coimbra","Coimbra"), ("Évora","Évora"),
        ("Faro","Faro"), ("Guarda","Guarda"), ("Leiria","Leiria"), ("Lisboa","Lisboa"),
        ("Portalegre","Portalegre"), ("Porto","Porto"), ("Santarém","Santarém"),
        ("Setúbal","Setúbal"), ("Viana do Castelo","Viana do Castelo"),
        ("Vila Real","Vila Real"), ("Viseu","Viseu"),
        ("Açores","Açores"), ("Madeira","Madeira")
    };

    public static bool IsValidCountry(string? country) => country is Brazil or Portugal;

    public static (string Code, string Name)[] RegionsFor(string? country) =>
        country == Portugal ? PortugalRegions : States;

    public static bool IsValidRegion(string? country, string? region) =>
        IsValidCountry(country) && !string.IsNullOrWhiteSpace(region) &&
        RegionsFor(country).Any(x => x.Code.Equals(region.Trim(), StringComparison.OrdinalIgnoreCase));

    public static string? CanonicalRegion(string? country, string? region) =>
        RegionsFor(country).FirstOrDefault(x =>
            x.Code.Equals(region?.Trim(), StringComparison.OrdinalIgnoreCase)).Code;

    public static string RegionLabel(string? country) =>
        country == Portugal ? "Distrito / Região autónoma" : "Estado (UF)";

    public static string PostalLabel(string? country) =>
        country == Portugal ? "Código postal" : "CEP";

    public static string CountryName(string? country) =>
        country == Portugal ? "Portugal" : "Brasil";

    // Compatibilidade com chamadas antigas do RentAll.
    public static bool IsValidUf(string? uf) => IsValidRegion(Brazil, uf);
}
