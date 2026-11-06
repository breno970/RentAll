(() => {
  "use strict";

  // Purely local walkthrough. No card form, no fetch, no storage, and no
  // changes to any rental request or payment record.
  document.addEventListener("DOMContentLoaded", () => {
    const root = document.querySelector("[data-payment-example]");
    if (!root) return;
    const screens = [...root.querySelectorAll("[data-demo-screen]")];
    const steps = [...root.querySelectorAll("[data-demo-step]")];
    const acknowledge = root.querySelector("[data-demo-ack]");
    const notice = root.querySelector("[data-demo-ack-error]");
    const methodButtons = [...root.querySelectorAll("[data-demo-method]")];
    const card = root.querySelector("[data-demo-card]");
    const methodInfo = root.querySelector("[data-demo-method-info]");
    const result = root.querySelector("[data-demo-result]");
    const resultTitle = root.querySelector("[data-demo-result-title]");
    const resultText = root.querySelector("[data-demo-result-text]");
    const finished = root.querySelector("[data-demo-finish]");
    let method = "card";
    let pending = false;

    function show(stage) {
      screens.forEach(element => { element.hidden = Number(element.dataset.demoScreen) !== stage; });
      steps.forEach(element => {
        if (Number(element.dataset.demoStep) === stage) element.setAttribute("aria-current", "step");
        else element.removeAttribute("aria-current");
      });
      const title = root.querySelector('[data-demo-screen="' + stage + '"] h2');
      title?.focus({ preventScroll: true });
    }
    function selectMethod(value) {
      method = value;
      methodButtons.forEach(button => {
        const active = button.dataset.demoMethod === value;
        button.classList.toggle("is-selected", active);
        button.setAttribute("aria-pressed", String(active));
      });
      if (card) card.hidden = value !== "card";
      if (methodInfo) methodInfo.textContent = value === "card"
        ? "Num checkout real, o prestador processaria os dados do cartão numa página segura. Não pedimos nenhum número nesta demonstração."
        : "Esta opção é meramente ilustrativa. Não será apresentado QR code, solicitado telemóvel nem efetuada comunicação de pagamento.";
    }
    function simulate(approved) {
      if (pending) return;
      pending = true;
      show(2);
      result?.classList.remove("is-declined");
      if (resultTitle) resultTitle.textContent = "A simular o processamento...";
      if (resultText) resultText.textContent = "Sem ligação à Stripe, a bancos ou a qualquer serviço externo.";
      if (finished) finished.hidden = true;
      setTimeout(() => {
        if (resultTitle) resultTitle.textContent = approved
          ? "Pagamento de exemplo autorizado"
          : "Pagamento de exemplo recusado";
        if (resultText) resultText.textContent = approved
          ? "Esta é apenas uma resposta fictícia. Não houve cobrança, reserva, transferência nem alteração ao pedido."
          : "Exemplo de recusa. Num checkout real, o utilizador poderia tentar outro método, sem confirmação de reserva.";
        if (finished) finished.hidden = !approved;
        if (!approved) result?.classList.add("is-declined");
        pending = false;
      }, 650);
    }

    root.querySelector("[data-demo-next]")?.addEventListener("click", () => {
      if (!acknowledge?.checked) {
        if (notice) notice.hidden = false;
        acknowledge?.focus();
        return;
      }
      if (notice) notice.hidden = true;
      show(1);
    });
    acknowledge?.addEventListener("change", () => { if (notice) notice.hidden = true; });
    methodButtons.forEach(button => button.addEventListener("click", () => selectMethod(button.dataset.demoMethod || "card")));
    root.querySelector("[data-demo-back]")?.addEventListener("click", () => show(0));
    root.querySelector("[data-demo-authorize]")?.addEventListener("click", () => simulate(true));
    root.querySelector("[data-demo-decline]")?.addEventListener("click", () => simulate(false));
    root.querySelector("[data-demo-retry]")?.addEventListener("click", () => show(1));
    finished?.addEventListener("click", () => show(3));
    root.querySelector("[data-demo-restart]")?.addEventListener("click", () => {
      if (acknowledge) acknowledge.checked = false;
      selectMethod("card");
      show(0);
    });
    selectMethod("card");
    show(0);
  });
})();