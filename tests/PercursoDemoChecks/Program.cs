using aluguel.Models;
using aluguel.Services;

static void Check(bool value, string message)
{
    if (!value) throw new Exception("FAILED: " + message);
}
static void Fails(Action action, string message)
{
    try { action(); } catch (InvalidOperationException) { return; }
    throw new Exception("SHOULD FAIL: " + message);
}
var now = DateTime.UtcNow;
PercursoAluguerDemo a = new() { PedidoId = 10 };
Fails(() => PercursoDemoRegras.Atualizar(a, "aprovar", true, now, metodo: "cartao"),
    "owner cannot pay");
Fails(() => PercursoDemoRegras.Atualizar(a, "aprovar", false, now, metodo: "paypal"),
    "invalid method");
PercursoDemoRegras.Atualizar(a, "recusar", false, now, metodo: "cartao");
Check(a.Estado == "PagamentoRecusado", "failed checkout");
PercursoDemoRegras.Atualizar(a, "aprovar", false, now, metodo: "mbway");
Check(a.PagamentoEm.HasValue && a.Estado == "PagamentoSimulado", "successful simulated checkout");
Fails(() => PercursoDemoRegras.Atualizar(a, "receber", false, now), "no receipt before agreement");
Fails(() => PercursoDemoRegras.Atualizar(a, "propor", false, now,
    tipo: "em_maos", local: "Ponto público", entregaUtc: now.AddDays(2)), "renter cannot propose");
Fails(() => PercursoDemoRegras.Atualizar(a, "propor", true, now,
    tipo: "em_maos", local: "Ponto público", entregaUtc: now.AddDays(-1)), "past delivery date");
PercursoDemoRegras.Atualizar(a, "propor", true, now,
    tipo: "ponto_recolha", local: "Estação de Lisboa", entregaUtc: now.AddDays(2));
Check(a.Estado == "EntregaProposta", "delivery proposal saved");
Fails(() => PercursoDemoRegras.Atualizar(a, "aceitar", true, now), "owner cannot accept");
PercursoDemoRegras.Atualizar(a, "aceitar", false, now);
Check(a.Estado == "EntregaAcordada", "renter accepted proposal");
PercursoDemoRegras.Atualizar(a, "receber", false, now);
Check(a.Estado == "EntregaAcordada", "needs both handover confirmations");
Fails(() => PercursoDemoRegras.Atualizar(a, "receber", false, now), "no duplicate receipt");
PercursoDemoRegras.Atualizar(a, "entregar", true, now);
Check(a.Estado == "EmCurso", "handover completed");
PercursoDemoRegras.Atualizar(a, "incidente", false, now);
Fails(() => PercursoDemoRegras.Atualizar(a, "devolver", false, now), "incident blocks return");
Fails(() => PercursoDemoRegras.Atualizar(a, "resolver", false, now), "only owner closes incident");
PercursoDemoRegras.Atualizar(a, "resolver", true, now);
PercursoDemoRegras.Atualizar(a, "devolver", false, now);
Check(a.Estado == "DevolucaoPendente", "return begun");
Fails(() => PercursoDemoRegras.Atualizar(a, "concluir", false, now), "only owner closes return");
PercursoDemoRegras.Atualizar(a, "concluir", true, now);
Check(a.Estado == "Concluido", "rental concluded");
PercursoDemoRegras.Atualizar(a, "avaliar", false, now, avaliacao: 5);
Check(a.Avaliacao == 5, "rating stored");
Fails(() => PercursoDemoRegras.Atualizar(a, "avaliar", false, now, avaliacao: 1), "no repeat rating");
Fails(() => PercursoDemoRegras.Atualizar(a, "cancelar", false, now), "cannot refund after completion");
Check(a.Versao == 11, "each accepted transition increments concurrency version");
PercursoAluguerDemo cancel = new() { PedidoId = 11 };
PercursoDemoRegras.Atualizar(cancel, "aprovar", false, now, metodo: "cartao");
PercursoDemoRegras.Atualizar(cancel, "cancelar", false, now);
Check(cancel.Estado == "Cancelado", "simulation-only cancellation");
Fails(() => PercursoDemoRegras.Atualizar(cancel, "aprovar", false, now, metodo: "cartao"), "cannot reuse cancelled demo");
Console.WriteLine("RentAll persistent demo rules and account role restrictions passed.");
