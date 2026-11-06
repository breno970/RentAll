(() => {
  "use strict";
  document.addEventListener("DOMContentLoaded", () => {
    const widget = document.querySelector("[data-rental-calculator]");
    const days = document.querySelector("[data-rental-days]");
    if (!widget || !days) return;
    const daily = Number(widget.dataset.dailyPrice);
    const currency = widget.dataset.country === "BR" ? "BRL" : "EUR";
    const locale = currency === "BRL" ? "pt-BR" : "pt-PT";
    const formatter = new Intl.NumberFormat(locale, { style: "currency", currency });
    function refresh() {
      const n = Number(days.value);
      if (!Number.isInteger(n) || n < 1 || n > Number(days.max)) {
        widget.textContent = "Seleciona entre 1 e " + days.max + " dias.";
        return;
      }
      widget.textContent = "Estimativa: " + formatter.format(Math.round(daily * n * 100) / 100)
        + " para " + n + (n === 1 ? " dia." : " dias.") + " Sem pagamento agora.";
    }
    days.addEventListener("input", refresh);
    refresh();
  });
})();