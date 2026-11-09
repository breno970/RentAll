(() => {
  "use strict";

  // Browser-only showcase. No fetch, card capture, storage, Stripe API or DB mutations.
  document.addEventListener("DOMContentLoaded", () => {
    const root = document.querySelector("[data-payment-example]");
    const flow = window.RentAllDemoFlow;
    if (!root || !flow) return;
    let state = flow.start();
    const q = (selector) => root.querySelector(selector);
    const qa = (selector) => [...root.querySelectorAll(selector)];
    const error = q("[data-demo-error]");
    const warnings = q("[data-demo-incident-status]");
    const isCurrent = (n) => state.step === n;

    function setText(selector, value) {
      qa(selector).forEach(element => { element.textContent = value; });
    }
    function setVisible(selector, visible) {
      qa(selector).forEach(element => { element.hidden = !visible; });
    }
    function formatDate(iso) {
      const date = new Date(iso);
      return Number.isFinite(date.valueOf())
        ? new Intl.DateTimeFormat("pt-PT", { dateStyle: "long", timeStyle: "short" }).format(date)
        : "Horário não definido";
    }
    function notice(message) {
      if (!error) return;
      error.textContent = message;
      error.hidden = !message;
      if (message) error.scrollIntoView({ block: "nearest" });
    }
    function render() {
      qa("[data-demo-screen]").forEach(element => {
        element.hidden = Number(element.dataset.demoScreen) !== state.step;
      });
      qa("[data-demo-step]").forEach(element => {
        const number = Number(element.dataset.demoStep);
        if (number === state.step) element.setAttribute("aria-current", "step");
        else element.removeAttribute("aria-current");
        element.classList.toggle("is-complete", number < state.step && state.step !== 8);
      });

      const heading = q('[data-demo-screen="' + state.step + '"] h2');
      heading?.focus({ preventScroll: true });

      setVisible("[data-demo-card]", state.method === "card");
      qa("[data-demo-method]").forEach(button => {
        const selected = button.dataset.demoMethod === state.method;
        button.classList.toggle("is-selected", selected);
        button.setAttribute("aria-pressed", String(selected));
      });
      setText("[data-demo-method-info]", state.method === "card"
        ? "A página de demonstração nunca solicita dados bancários; numa transação real o checkout seria servido pelo prestador."
        : "MB WAY apenas ilustrativo: não há pedido ao telemóvel, QR code ou contacto a serviços externos.");

      if (isCurrent(2)) {
        const success = state.approved === true;
        setText("[data-demo-result-title]", success
          ? "Pagamento aprovado — APENAS DEMONSTRAÇÃO"
          : "Pagamento recusado — APENAS DEMONSTRAÇÃO");
        setText("[data-demo-result-text]", success
          ? "A autorização foi emulada. O cenário pode avançar para entrega, mas não existe cobrança, reserva real ou sessão Stripe."
          : "A recusa foi emulada. Podes voltar a experimentar o checkout, sem alteração ao pedido nem qualquer cobrança.");
        q("[data-demo-result]")?.classList.toggle("is-declined", !success);
        setVisible("[data-demo-after-pay]", success);
        setVisible("[data-demo-retry]", !success);
      }

      if (isCurrent(3)) {
        setVisible("[data-demo-proposal]", !!state.delivery);
        if (state.delivery) {
          const names = {
            em_maos: "Entrega em mãos num local público",
            ponto_recolha: "Ponto de recolha",
            transportadora: "Transportadora (sem envio efetivo)"
          };
          setText("[data-demo-proposal-text]", names[state.delivery.kind] + " · " +
            state.delivery.place + " · " + formatDate(state.delivery.at));
        }
      }
      if (isCurrent(4) && state.delivery) {
        setText("[data-demo-delivery-review]", "Acordo de exemplo: " + state.delivery.place +
          ", " + formatDate(state.delivery.at) +
          ". Confirma o estado e os acessórios antes de entregar.");
      }
      ["owner", "renter"].forEach(actor => {
        setText("[data-demo-handover-" + actor + "]", state.handover[actor] ? "Confirmado (simulado)" : "Por confirmar");
        setText("[data-demo-return-" + actor + "]", state.returned[actor] ? "Confirmado (simulado)" : "Por confirmar");
        qa('[data-demo-handover="' + actor + '"]').forEach(button => { button.disabled = state.handover[actor]; });
        qa('[data-demo-return="' + actor + '"]').forEach(button => { button.disabled = state.returned[actor]; });
      });

      setVisible("[data-demo-resolve-incident]", state.incident);
      setVisible("[data-demo-report-incident]", !state.incident);
      setText("[data-demo-incident-status]", state.incident
        ? "Ocorrência de exemplo aberta. A devolução/fecho ficam suspensos até ser resolvida."
        : "");
      setText("[data-demo-rating-result]", state.rating
        ? "Avaliação demonstrativa de " + state.rating + "/5 registada apenas nesta página."
        : "");
      const activity = q("[data-demo-history]");
      if (activity) {
        activity.replaceChildren();
        state.history.slice().reverse().forEach(message => {
          const li = document.createElement("li");
          li.textContent = message;
          activity.append(li);
        });
      }
    }
    function commit(action, data) {
      try {
        state = flow.apply(state, action, data);
        notice("");
        render();
      } catch (ex) {
        notice(ex instanceof Error ? ex.message : "Não foi possível avançar nesta demonstração.");
      }
    }
    const localInputDate = (date) => {
      const offset = date.getTimezoneOffset();
      return new Date(date.getTime() - offset * 60000).toISOString().slice(0, 16);
    };
    const deliveryDate = q("[data-demo-delivery-time]");
    if (deliveryDate) {
      deliveryDate.min = localInputDate(new Date(Date.now() + 60000));
      deliveryDate.value = localInputDate(new Date(Date.now() + 86400000));
    }

    q("[data-demo-next]")?.addEventListener("click", () =>
      commit("AGREE", { accepted: q("[data-demo-ack]")?.checked === true }));
    q("[data-demo-back]")?.addEventListener("click", () => commit("RESTART"));
    qa("[data-demo-method]").forEach(button => button.addEventListener("click", () =>
      commit("SET_METHOD", { method: button.dataset.demoMethod })));
    q("[data-demo-authorize]")?.addEventListener("click", () => commit("PAY", { approved: true }));
    q("[data-demo-decline]")?.addEventListener("click", () => commit("PAY", { approved: false }));
    q("[data-demo-retry]")?.addEventListener("click", () => commit("RETRY"));
    q("[data-demo-after-pay]")?.addEventListener("click", () => commit("CONTINUE"));
    q("[data-demo-delivery-form]")?.addEventListener("submit", event => {
      event.preventDefault();
      commit("PROPOSE_DELIVERY", {
        kind: q("[data-demo-delivery-kind]")?.value,
        place: q("[data-demo-delivery-place]")?.value,
        at: q("[data-demo-delivery-time]")?.value
      });
    });
    q("[data-demo-confirm-delivery]")?.addEventListener("click", () => commit("CONFIRM_DELIVERY"));

    qa("[data-demo-handover]").forEach(button => button.addEventListener("click", () => {
      if (!q("[data-demo-handover-check]")?.checked) {
        notice("Confirma primeiro a verificação ilustrativa do estado e acessórios.");
        return;
      }
      commit("HANDOVER", { actor: button.dataset.demoHandover });
    }));
    q("[data-demo-start-return]")?.addEventListener("click", () => commit("START_RETURN"));
    qa("[data-demo-return]").forEach(button => button.addEventListener("click", () => {
      if (!q("[data-demo-return-check]")?.checked) {
        notice("Confirma primeiro a verificação ilustrativa da devolução.");
        return;
      }
      commit("RETURN", { actor: button.dataset.demoReturn });
    }));
    qa("[data-demo-report-incident]").forEach(button =>
      button.addEventListener("click", () => commit("REPORT_INCIDENT")));
    qa("[data-demo-resolve-incident]").forEach(button =>
      button.addEventListener("click", () => commit("RESOLVE_INCIDENT")));
    q("[data-demo-refund]")?.addEventListener("click", () => commit("CANCEL_REFUND"));
    q("[data-demo-rate]")?.addEventListener("click", () => {
      const value = Number(q("[data-demo-rating]")?.value);
      commit("RATE", { rating: value });
    });
    qa("[data-demo-restart]").forEach(button => button.addEventListener("click", () => {
      const check = q("[data-demo-ack]");
      if (check) check.checked = false;
      const deliveryCheck = q("[data-demo-handover-check]");
      if (deliveryCheck) deliveryCheck.checked = false;
      const returnCheck = q("[data-demo-return-check]");
      if (returnCheck) returnCheck.checked = false;
      if (q("[data-demo-delivery-place]")) q("[data-demo-delivery-place]").value = "";
      if (q("[data-demo-rating]")) q("[data-demo-rating]").value = "";
      commit("RESTART");
    }));
    render();
  });
})();
