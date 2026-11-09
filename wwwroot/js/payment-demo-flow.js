/* RentAll demonstration state machine.
   No API requests, payment tokens, persistence or bank interactions.
   Every state is illustrative and cannot change a real rental. */
(function (root, factory) {
  "use strict";
  const api = factory();
  if (typeof module === "object" && module.exports) module.exports = api;
  else root.RentAllDemoFlow = api;
})(typeof globalThis === "object" ? globalThis : this, function () {
  "use strict";
  const deliveryTypes = ["em_maos", "ponto_recolha", "transportadora"];
  const actors = ["owner", "renter"];
  function start() {
    return { step: 0, method: "card", approved: null, delivery: null,
      deliveryAgreed: false, handover: { owner: false, renter: false },
      returned: { owner: false, renter: false }, incident: false, rating: null,
      history: ["Demonstração iniciada (sem pagamento ou reserva real)."] };
  }
  function apply(previous, type, payload = {}) {
    if (!previous || typeof previous !== "object") throw new Error("Estado de demonstração inválido.");
    if (type === "RESTART") return start();
    const state = {
      ...previous, handover: { ...previous.handover },
      returned: { ...previous.returned },
      history: [...previous.history]
    };
    const requireStep = (n) => { if (state.step !== n) throw new Error("Ação indisponível nesta etapa."); };
    const record = (message) => state.history.push(message);
    const validActor = (name) => { if (!actors.includes(name)) throw new Error("Participante inválido."); };
    switch (type) {
      case "AGREE":
        requireStep(0);
        if (payload.accepted !== true) throw new Error("Confirma primeiro que é uma demonstração.");
        state.step = 1;
        record("Condições ilustrativas revistas.");
        break;
      case "SET_METHOD":
        requireStep(1);
        if (!["card", "mbway"].includes(payload.method)) throw new Error("Método inválido.");
        state.method = payload.method;
        break;
      case "PAY":
        requireStep(1);
        if (typeof payload.approved !== "boolean") throw new Error("Resultado inválido.");
        state.approved = payload.approved;
        state.step = 2;
        record(payload.approved ? "Checkout de demonstração: aprovação simulada (0 € cobrados)." :
          "Checkout de demonstração: pagamento recusado (0 € cobrados).");
        break;
      case "RETRY":
        requireStep(2);
        if (state.approved !== false) throw new Error("Não existe recusa para repetir.");
        state.step = 1;
        break;
      case "CONTINUE":
        requireStep(2);
        if (!state.approved) throw new Error("É necessária uma aprovação simulada.");
        state.step = 3;
        break;
      case "PROPOSE_DELIVERY": {
        requireStep(3);
        if (!deliveryTypes.includes(payload.kind)) throw new Error("Escolhe uma modalidade de entrega.");
        const place = String(payload.place || "").trim();
        if (place.length < 3 || place.length > 120) throw new Error("Indica um local público ou ponto de recolha (3–120 caracteres).");
        const at = String(payload.at || "");
        const timestamp = Date.parse(at);
        if (!Number.isFinite(timestamp) || timestamp <= Date.now()) throw new Error("Seleciona uma data e hora futuras.");
        state.delivery = { kind: payload.kind, place, at };
        state.deliveryAgreed = false;
        record("Anunciante propôs uma modalidade e horário de entrega (simulados).");
        break;
      }
      case "CONFIRM_DELIVERY":
        requireStep(3);
        if (!state.delivery) throw new Error("O anunciante deve propor primeiro a entrega.");
        state.deliveryAgreed = true;
        state.step = 4;
        record("Locatário aceitou as condições ilustrativas da entrega.");
        break;
      case "HANDOVER":
        requireStep(4);
        validActor(payload.actor);
        if (state.handover[payload.actor]) throw new Error("Este participante já confirmou.");
        state.handover[payload.actor] = true;
        record((payload.actor === "owner" ? "Anunciante" : "Locatário") + " confirmou a entrega de exemplo.");
        if (state.handover.owner && state.handover.renter) {
          state.step = 5;
          record("Entrega concluída na demonstração; aluguer em curso.");
        }
        break;
      case "START_RETURN":
        requireStep(5);
        if (state.incident) throw new Error("Resolve primeiro a ocorrência demonstrativa.");
        state.step = 6;
        record("Locatário iniciou a devolução de exemplo.");
        break;
      case "RETURN":
        requireStep(6);
        if (state.incident) throw new Error("Resolve primeiro a ocorrência demonstrativa.");
        validActor(payload.actor);
        if (state.returned[payload.actor]) throw new Error("Este participante já confirmou.");
        state.returned[payload.actor] = true;
        record((payload.actor === "owner" ? "Anunciante" : "Locatário") + " confirmou a devolução de exemplo.");
        if (state.returned.owner && state.returned.renter) {
          state.step = 7;
          record("Aluguer demonstrativo concluído. Nenhum valor transferido.");
        }
        break;
      case "REPORT_INCIDENT":
        if (state.step !== 5 && state.step !== 6) throw new Error("Ocorrência indisponível nesta etapa.");
        if (state.incident) throw new Error("Já existe uma ocorrência demonstrativa.");
        state.incident = true;
        record("Ocorrência simulada aberta para revisão.");
        break;
      case "RESOLVE_INCIDENT":
        if (!state.incident || (state.step !== 5 && state.step !== 6)) throw new Error("Não existe ocorrência a resolver.");
        state.incident = false;
        record("Ocorrência simulada resolvida sem movimentos financeiros.");
        break;
      case "CANCEL_REFUND":
        requireStep(3);
        if (!state.approved || state.deliveryAgreed) throw new Error("Cancelamento demonstrativo indisponível.");
        state.step = 8;
        record("Cancelamento e reembolso integral simulados. Não houve cobrança nem reembolso real.");
        break;
      case "RATE":
        requireStep(7);
        if (!Number.isInteger(payload.rating) || payload.rating < 1 || payload.rating > 5)
          throw new Error("Escolhe uma avaliação de 1 a 5.");
        state.rating = payload.rating;
        record("Avaliação ilustrativa atribuída: " + payload.rating + "/5.");
        break;
      default:
        throw new Error("Ação de demonstração desconhecida.");
    }
    return state;
  }
  return Object.freeze({ start, apply });
});
