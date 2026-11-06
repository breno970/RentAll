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
