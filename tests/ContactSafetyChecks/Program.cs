using aluguel.Services;
using Reason = aluguel.Services.MarketplaceContactSafety.Reason;

var cases = new (string Text, Reason Expected)[]
{
    ("915 123 456", Reason.Phone),
    ("+351 912-345-678", Reason.Phone),
    ("9 1 2 3 4 5 6 7 8", Reason.Phone),
    ("9\u200b1\u200b2\u200b3\u200b4\u200b5\u200b6\u200b7\u200b8", Reason.Phone),
    ("nove um dois tres quatro cinco seis sete oito", Reason.Phone),
    ("nove um dois três quatro cinco seis sete oito", Reason.Phone),
    ("nome@gmail.com", Reason.Email),
    ("nome arroba gmail ponto com", Reason.Email),
    ("https://example.com", Reason.Link),
    ("www.example.pt", Reason.Link),
    ("sitemeu.pt", Reason.Link),
    ("t.me/contacto", Reason.Link),
    ("@contacto_aqui", Reason.Social),
    ("Fala no Instagram", Reason.Social),
    ("Passa para o WhatsApp", Reason.Social),
    ("Paga por MB WAY", Reason.ExternalPayment),
    ("Podes pagar em Pix", Reason.ExternalPayment),
    ("Envia o IBAN", Reason.ExternalPayment),
    ("Alugo Nikon Z9 por 5 dias", Reason.None),
    ("Tenho uma câmara Sony A7 III", Reason.None),
    ("O artigo está disponível na próxima semana?", Reason.None),
    ("Entrega numa estação pública, sábado às 18h.", Reason.None),
    ("Ferramenta de 12 cm para 2 dias", Reason.None),
    ("", Reason.None)
};
var passed = 0;
foreach (var (value, expected) in cases)
{
    var actual = MarketplaceContactSafety.Classify(value);
    if (actual != expected)
        throw new Exception($"Unexpected classification for {value}: expected {expected}, actual {actual}");
    passed++;
}
Console.WriteLine($"Marketplace contact checks passed: {passed}.");

var portugalDefault = new aluguel.ViewModel.MarketplaceFilters();
portugalDefault.Normalize();
if (portugalDefault.Pais != "PT") throw new Exception("Portugal must be the default market.");
var brazil = new aluguel.ViewModel.MarketplaceFilters { Pais = "BR", Estado = "RJ", MinPrice = 30, MaxPrice = 10 };
brazil.Normalize();
if (brazil.Pais != "PT" || brazil.Estado != null || brazil.MinPrice != 10 || brazil.MaxPrice != 30)
    throw new Exception("Portugal-only browsing must ignore Brazil and invalid region filters while preserving prices.");
var wrongRegion = new aluguel.ViewModel.MarketplaceFilters { Pais = "PT", Estado = "RJ" };
wrongRegion.Normalize();
if (wrongRegion.Estado != null) throw new Exception("Reject regions that belong to the other country.");
var portugal = new aluguel.ViewModel.MarketplaceFilters { Pais = "PT", Estado = "Lisboa" };
portugal.Normalize();
if (portugal.Estado != "Lisboa") throw new Exception("Keep a valid Portuguese district.");
Console.WriteLine("Portugal-only country, district and price filters passed.");

var validListings = new (string? Title, string? Description, bool Valid)[]
{
    ("Câmara Sony", "Disponível para alugar em Lisboa.", true),
    (new string('T', 140), new string('D', 6000), true),
    (new string('T', 141), "Descrição", false),
    ("Artigo", new string('D', 6001), false),
    ("   ", "Descrição", false),
    ("Artigo", "   ", false),
    (null, "Descrição", false),
    ("Artigo", null, false)
};
foreach (var (title, description, expected) in validListings)
{
    bool actual = ListingContentValidation.ValidTitle(title)
        && ListingContentValidation.ValidDescription(description);
    if (actual != expected)
        throw new Exception($"Unexpected listing content validation outcome: expected {expected}, got {actual}.");
}
Console.WriteLine($"Listing content boundaries passed: {validListings.Length}.");

// Upload content must match the declared image type (Content-Type is client-controlled).
var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13 };
var jpeg = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
var webp = "RIFF\0\0\0\0WEBPVP8 "u8.ToArray();
var html = "<html><script>"u8.ToArray();
if (!aluguel.Services.ImageSignature.Matches("image/png", png)) throw new Exception("PNG rejected");
if (!aluguel.Services.ImageSignature.Matches("image/jpeg", jpeg)) throw new Exception("JPEG rejected");
if (!aluguel.Services.ImageSignature.Matches("image/webp", webp)) throw new Exception("WebP rejected");
if (aluguel.Services.ImageSignature.Matches("image/png", html)) throw new Exception("HTML accepted as PNG");
if (aluguel.Services.ImageSignature.Matches("image/jpeg", png)) throw new Exception("PNG accepted as JPEG");
if (aluguel.Services.ImageSignature.Matches("image/gif", png)) throw new Exception("Unsupported type accepted");
if (aluguel.Services.ImageSignature.Matches("image/png", Array.Empty<byte>())) throw new Exception("Empty file accepted");
Console.WriteLine("Image signature checks passed: 7.");
