namespace aluguel.ViewModel;

public static class RentalJourneyProgress
{
    public static string Label(string? stage) => stage switch
    {
        "Checkout" => "A aguardar pagamento simulado",
        "PagamentoRecusado" => "Pagamento simulado recusado",
        "PagamentoSimulado" => "Pagamento simulado aprovado",
        "EntregaProposta" => "Entrega proposta",
        "EntregaAcordada" => "Entrega acordada",
        "EmCurso" => "Receção confirmada na simulação",
        "DevolucaoPendente" => "Devolução simulada por confirmar",
        "Concluido" => "Aluguer demonstrativo concluído",
        "Cancelado" => "Aluguer cancelado",
        _ => "Pedido aceite"
    };

    public static string Action(string? stage, bool owner) => (stage, owner) switch
    {
        (null or "Checkout" or "PagamentoRecusado", false) => "Ir para pagamento simulado",
        ("PagamentoSimulado" or "EntregaProposta", _) => "Acompanhar a entrega",
        ("EntregaAcordada", false) => "Ver entrega e confirmar receção",
        ("EntregaAcordada", true) => "Ver e confirmar entrega",
        ("EmCurso" or "DevolucaoPendente", _) => "Acompanhar devolução",
        ("Concluido" or "Cancelado", _) => "Ver resumo do aluguer",
        _ => "Acompanhar aluguer"
    };
}
