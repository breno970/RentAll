(() => {
  "use strict";
  document.addEventListener("DOMContentLoaded", () => {
    const root = document.querySelector("[data-rental-prototype]");
    if (!root) return;
    const format = n => new Intl.NumberFormat("pt-PT", { style: "currency", currency: "EUR" }).format(n);
    const tabs = [...root.querySelectorAll("[data-proto-tab]")];
    const panels = [...root.querySelectorAll("[data-proto-panel]")];
    const type = root.querySelector("[data-proto-type]");
    const price = root.querySelector("[data-proto-price]");
    const days = root.querySelector("[data-proto-days]");
    const value = root.querySelector("[data-proto-value]");
    const outcome = root.querySelector("[data-proto-outcome]");
    const handover = root.querySelector("[data-proto-handover]");
    const returning = root.querySelector("[data-proto-return]");
    const feedbackHandover = root.querySelector("[data-proto-handover-status]");
    const feedbackReturn = root.querySelector("[data-proto-return-status]");
    const resultTitle = root.querySelector("[data-proto-result-title]");
    const resultText = root.querySelector("[data-proto-result-text]");
    const revenueSlider = root.querySelector("[data-proto-volume]");
    const presets = {
      drill: { daily: 18, value: 300 },
      camera: { daily: 35, value: 900 },
      bike: { daily: 14, value: 400 }
    };
    const number = (element, fallback, min, max) => {
      const parsed = Number(element?.value);
      return Number.isFinite(parsed) && parsed >= min && parsed <= max ? parsed : fallback;
    };
    let activeStep = 0;
    let result = null;

    function updateCosts() {
      const rent = number(price, 18, 1, 1000) * number(days, 3, 1, 30);
      const fee = Math.round(rent * 10) / 100;
      // A cautious, illustrative 30%-of-value limit; never a legal
      // or Stripe authorization guarantee.
      const deposit = Math.round(number(value, 300, 20, 5000) * 0.30 * 100) / 100;
      root.querySelectorAll("[data-proto-rent]").forEach(x => { x.textContent = format(rent); });
      root.querySelectorAll("[data-proto-fee]").forEach(x => { x.textContent = format(fee); });
      root.querySelectorAll("[data-proto-owner]").forEach(x => { x.textContent = format(rent - fee); });
      root.querySelectorAll("[data-proto-deposit]").forEach(x => { x.textContent = format(deposit); });
    }

    function switchTo(step, focus = true) {
      activeStep = Math.max(0, Math.min(5, step));
      tabs.forEach(tab => {
        const selected = Number(tab.dataset.protoTab) === activeStep;
        if (selected) tab.setAttribute("aria-current", "step");
        else tab.removeAttribute("aria-current");
      });
      panels.forEach(panel => { panel.hidden = Number(panel.dataset.protoPanel) !== activeStep; });
      if (focus) root.querySelector('[data-proto-panel="' + activeStep + '"] h3')?.focus({ preventScroll: true });
    }
    function allChecked(list) {
      const checks = [...(list?.querySelectorAll('input[type="checkbox"]') || [])];
      return checks.length > 0 && checks.every(c => c.checked);
    }
    function updateResult() {
      const scenarios = {
        ok: ["Devolução sem ocorrências — exemplo", "O modelo futuro permitiria registar a aceitação bilateral do estado final e iniciar a libertação de eventual caução, respeitando os prazos do prestador. Nenhum valor foi retido ou libertado nesta página."],
        scratch: ["Dano comunicado — análise necessária", "Registam-se fotografias comparativas, descrição e resposta do outro participante. O desgaste normal deve ser distinguido de um dano novo. Uma eventual compensação exige provas, regras claras e decisão contestável; não é automaticamente retirada da caução."],
        late: ["Atraso comunicado — negociação e prazos", "O sistema notificaria o utilizador, ofereceria eventual prolongamento acordado e aplicaria apenas encargos previamente aceites e legalmente permitidos. Um atraso não constitui, por si só, furto."],
        missing: ["Não devolução — incidente grave", "O anunciante comunicaria a falta de devolução com o contrato e provas disponíveis. A equipa deveria analisar, proteger dados e indicar as autoridades competentes perante suspeita de crime. Não é possível garantir recuperação do artigo nem compensação automática."]
      };
      const [title, text] = scenarios[result || "ok"];
      if (resultTitle) resultTitle.textContent = title;
      if (resultText) resultText.textContent = text;
    }
    function reset() {
      if (type) type.value = "drill";
      if (price) price.value = "18";
      if (days) days.value = "3";
      if (value) value.value = "300";
      if (outcome) outcome.value = "ok";
      root.querySelectorAll('.ra-proto-checklist input[type="checkbox"]').forEach(x => { x.checked = false; });
      if (feedbackHandover) feedbackHandover.textContent = "";
      if (feedbackReturn) feedbackReturn.textContent = "";
      result = null;
      if (resultTitle) resultTitle.textContent = "Escolhe um resultado de devolução";
      if (resultText) resultText.textContent = "Volta à etapa anterior e seleciona uma ocorrência para explorar os próximos passos.";
      updateCosts();
      switchTo(0);
    }
    tabs.forEach(tab => tab.addEventListener("click", () => switchTo(Number(tab.dataset.protoTab))));
    root.querySelectorAll("[data-proto-next]").forEach(button => button.addEventListener("click", () => switchTo(activeStep + 1)));
    root.querySelectorAll("[data-proto-reset]").forEach(button => button.addEventListener("click", reset));
    type?.addEventListener("change", () => {
      const preset = presets[type.value] || presets.drill;
      if (price) price.value = String(preset.daily);
      if (value) value.value = String(preset.value);
      updateCosts();
    });
    [price, days, value].forEach(input => input?.addEventListener("input", updateCosts));
    root.querySelector("[data-proto-confirm-handover]")?.addEventListener("click", () => {
      if (!allChecked(handover)) {
        if (feedbackHandover) feedbackHandover.textContent = "Assinala os quatro pontos do exemplo para avançar.";
        return;
      }
      if (feedbackHandover) feedbackHandover.textContent = "Entrega simulada. Nenhum registo foi criado.";
      switchTo(4);
    });
    root.querySelector("[data-proto-confirm-return]")?.addEventListener("click", () => {
      if (!allChecked(returning)) {
        if (feedbackReturn) feedbackReturn.textContent = "Completa os dois pontos de comparação antes de avançar.";
        return;
      }
      result = outcome?.value || "ok";
      updateResult();
      if (feedbackReturn) feedbackReturn.textContent = "Resultado fictício preparado, sem registo de incidente.";
      switchTo(5);
    });
    const updateRevenue = () => {
      const count = number(revenueSlider, 40, 0, 200);
      const countView = root.querySelector("[data-proto-count]");
      const revenueView = root.querySelector("[data-proto-revenue]");
      if (countView) countView.textContent = String(count);
      if (revenueView) revenueView.textContent = format(count * 45 * 0.10);
    };
    revenueSlider?.addEventListener("input", updateRevenue);
    updateCosts();
    updateRevenue();
    switchTo(0, false);
  });
})();