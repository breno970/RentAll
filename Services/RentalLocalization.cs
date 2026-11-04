using System.Globalization;

namespace aluguel.Services;

/// <summary>Apresentação de preços sem conversão cambial automática.</summary>
public static class RentalLocalization
{
    public static string FormatPrice(float value, string? country)
    {
        var locale = country == LocationCatalog.Portugal ? "pt-PT" : "pt-BR";
        return value.ToString("C2", CultureInfo.GetCultureInfo(locale));
    }

    public static string CurrencyCode(string? country) =>
        country == LocationCatalog.Portugal ? "EUR" : "BRL";

    public static string RegionDisplay(string? country, string? region, string? city) =>
        string.Join(", ", new[] { city, region, LocationCatalog.CountryName(country) }
            .Where(x => !string.IsNullOrWhiteSpace(x)));
}
