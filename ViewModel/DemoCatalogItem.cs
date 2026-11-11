namespace aluguel.ViewModel;

public sealed record DemoCatalogItem(
    int CategoryId, string Slug, string Name, string Description, float DailyPrice, string City, string Photo)
{
    public static IReadOnlyList<DemoCatalogItem> All { get; } = Array.AsReadOnly(new DemoCatalogItem[]
    {
        new(5, "berbequim", "Berbequim sem fios", "Berbequim 18 V com bateria e mala", 11, "Lisboa", "ferramentas.jpg"),
        new(2, "projetor", "Projetor Full HD", "Projetor portátil com HDMI", 22, "Coimbra", "projetor.jpg"),
        new(4, "paddle", "Prancha de paddle insuflável", "Prancha com bomba e remo", 23, "Faro", "paddle.jpg"),
        new(1, "switch", "Consola Nintendo Switch OLED", "Consola com dois comandos", 18, "Lisboa", "switch.jpg"),
        new(6, "robot-cozinha", "Robot de cozinha", "Robot com taça e acessórios", 21, "Porto", "robot-cozinha.jpg"),
        new(3, "piano", "Teclado digital 88 teclas", "Teclado com suporte", 24, "Lisboa", "piano.jpg"),
        new(2, "camara", "Câmara Sony Alpha", "Câmara com objetiva 18–55 mm", 32, "Lisboa", "camera.jpg"),
        new(4, "tenda", "Tenda de campismo para 4", "Tenda familiar de montagem rápida", 14, "Coimbra", "tenda.jpg"),
        new(1, "ps5", "PlayStation 5 Slim", "Consola com comando", 25, "Porto", "consolas.jpg"),
        new(1, "xbox", "Xbox Series S", "Consola compacta de 512 GB", 20, "Braga", "xbox.jpg"),
        new(1, "realidade-virtual", "Óculos de realidade virtual", "Óculos com dois comandos", 19, "Braga", "realidade-virtual.jpg"),
        new(2, "camara-acao", "Câmara de ação GoPro", "Câmara com caixa estanque", 16, "Faro", "camara-acao.jpg"),
        new(3, "guitarra", "Guitarra acústica", "Guitarra com capa", 12, "Porto", "guitarra.jpg"),
        new(3, "coluna", "Coluna amplificada", "Coluna portátil Bluetooth", 15, "Setúbal", "coluna.jpg"),
        new(3, "costura", "Máquina de costura", "Máquina compacta com pedal", 12, "Coimbra", "costura.jpg"),
        new(4, "bicicleta", "Bicicleta de montanha", "Bicicleta com capacete", 19, "Braga", "bicicleta.jpg"),
        new(5, "lavadora", "Máquina de lavar a pressão", "Lavadora com mangueira e lança", 17, "Porto", "lavadora.jpg"),
        new(5, "escadote", "Escadote telescópico", "Escadote em alumínio de 3,8 m", 10, "Setúbal", "escadote.jpg"),
        new(5, "aspirador", "Aspirador de oficina", "Aspirador de sólidos e líquidos", 15, "Porto", "aspirador.jpg"),
        new(6, "cafe", "Máquina de café automática", "Máquina de café com depósito de 1,5 L", 16, "Lisboa", "cafe.jpg"),
        new(6, "desumidificador", "Desumidificador portátil", "Desumidificador com depósito de 2 L", 13, "Coimbra", "desumidificador.jpg"),
        new(6, "air-fryer", "Fritadeira de ar quente", "Fritadeira de bancada com cesto", 11, "Setúbal", "air-fryer.jpg")
    });

    public static DemoCatalogItem? ForListing(string? name) =>
        name?.StartsWith("[DEMO] ", StringComparison.Ordinal) == true
            ? All.FirstOrDefault(item => name == "[DEMO] " + item.Name)
            : null;
}
