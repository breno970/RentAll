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

// Logistics: change requests, checklists, accessories, notes and return discrepancies.
PercursoAluguerDemo l = new() { PedidoId = 12 };
PercursoDemoRegras.Atualizar(l, "aprovar", false, now, metodo: "cartao");
Fails(() => PercursoDemoRegras.Atualizar(l, "pedir_alteracao", false, now, nota: "19h?"), "no change request without proposal");
PercursoDemoRegras.Atualizar(l, "propor", true, now, tipo: "em_maos", local: "Estação do Oriente", entregaUtc: now.AddDays(1));
Fails(() => PercursoDemoRegras.Atualizar(l, "pedir_alteracao", true, now, nota: "Outra hora"), "owner cannot request change to own proposal");
Fails(() => PercursoDemoRegras.Atualizar(l, "pedir_alteracao", false, now, nota: " "), "change request needs a reason");
PercursoDemoRegras.Atualizar(l, "pedir_alteracao", false, now, nota: "Pode ser às 19h?");
Check(l.Estado == "PagamentoSimulado" && l.AcordadaEm == null, "change request reopens delivery proposal");
Fails(() => PercursoDemoRegras.Atualizar(l, "aceitar", false, now), "cannot accept a withdrawn proposal");
PercursoDemoRegras.Atualizar(l, "propor", true, now, tipo: "em_maos", local: "Estação do Oriente", entregaUtc: now.AddDays(1).AddMinutes(30));
PercursoDemoRegras.Atualizar(l, "aceitar", false, now);
Fails(() => PercursoDemoRegras.Atualizar(l, "checklist_entrega", true, now, estadoArtigo: "novo_em_folha"), "unknown condition rejected");
Fails(() => PercursoDemoRegras.Atualizar(l, "checklist_entrega", true, now, estadoArtigo: "bom", acessorios: new string('a', 301)), "accessories length limit");
Fails(() => PercursoDemoRegras.Atualizar(l, "checklist_entrega", true, now, estadoArtigo: "bom", nota: new string('n', 501)), "note length limit");
var before = l.Estado;
PercursoDemoRegras.Atualizar(l, "checklist_entrega", true, now, estadoArtigo: "marcas_uso", acessorios: "Cabo, comando", nota: "Risco na tampa");
PercursoDemoRegras.Atualizar(l, "checklist_entrega", false, now, estadoArtigo: "marcas_uso");
Check(l.Estado == before, "checklists never change the journey state");
Fails(() => PercursoDemoRegras.Atualizar(l, "checklist_devolucao", false, now, estadoArtigo: "bom"), "no return checklist before handover");
PercursoDemoRegras.Atualizar(l, "entregar", true, now);
PercursoDemoRegras.Atualizar(l, "receber", false, now);
Fails(() => PercursoDemoRegras.Atualizar(l, "checklist_entrega", true, now, estadoArtigo: "bom"), "no delivery checklist after handover");
PercursoDemoRegras.Atualizar(l, "checklist_devolucao", false, now, estadoArtigo: "bom", acessorios: "Cabo, comando");
Fails(() => PercursoDemoRegras.Atualizar(l, "divergencia", true, now, nota: "Falta cabo"), "no discrepancy before return");
PercursoDemoRegras.Atualizar(l, "devolver", false, now, nota: "Devolvido");
Fails(() => PercursoDemoRegras.Atualizar(l, "divergencia", false, now, nota: "Falta cabo"), "renter cannot raise return discrepancy");
Fails(() => PercursoDemoRegras.Atualizar(l, "divergencia", true, now), "discrepancy needs a description");
PercursoDemoRegras.Atualizar(l, "divergencia", true, now, nota: "Falta a bolsa");
Check(l.IncidenteAberto, "discrepancy opens an issue");
Fails(() => PercursoDemoRegras.Atualizar(l, "concluir", true, now), "discrepancy blocks conclusion");
PercursoDemoRegras.Atualizar(l, "resolver", true, now, nota: "Bolsa encontrada");
PercursoDemoRegras.Atualizar(l, "concluir", true, now);
Check(l.Estado == "Concluido", "concluded after resolving discrepancy");
Check(PercursoDemoRegras.ExigeHistorico("checklist_entrega") && !PercursoDemoRegras.ExigeHistorico("aprovar"), "trail requirement map");
Console.WriteLine("RentAll delivery/return logistics rules passed.");
