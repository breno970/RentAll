(() => {
  "use strict";
  document.addEventListener("DOMContentLoaded", () => {
    const form = document.querySelector("[data-single-publish]");
    if (!form) return;
    const price = form.querySelector("[data-money-input]");
    const country = form.querySelector("[data-region-country]");
    const output = form.querySelector("[data-earnings-preview]");
    if (!price || !country || !output) return;
    const update = () => {
      const input = String(price.value || "").trim();
      if (!input) { output.textContent = "Indica um preço para veres uma simulação de ganhos."; return; }
      const amount = input.replace(/[^0-9.,]/g,"");
      const comma = amount.lastIndexOf(",");
      const dot = amount.lastIndexOf(".");
      const normalized = comma !== -1 && dot !== -1
        ? (comma > dot ? amount.replace(/\./g,"").replace(",",".") : amount.replace(/,/g,""))
        : amount.replace(",",".");
      const daily = Number(normalized);
      if (!Number.isFinite(daily) || daily <= 0) {
        output.textContent = "Indica um valor positivo para a simulação.";
        return;
      }
      const br = country.value === "BR";
      const fmt = new Intl.NumberFormat(br ? "pt-BR" : "pt-PT", {
        style: "currency", currency: br ? "BRL" : "EUR", maximumFractionDigits: 2
      });
      const total = Math.round(daily * 3 * 100) / 100;
      output.textContent = "Exemplo: 3 dias a " + fmt.format(daily) + " = " +
        fmt.format(total) + " de valor bruto. Num modelo futuro com 10% de comissão, " +
        "receberias cerca de " + fmt.format(Math.round(total * .9 * 100) / 100) +
        " antes de eventuais impostos e outros custos. Hoje não é cobrada qualquer comissão.";
    };
    price.addEventListener("input", update);
    price.addEventListener("blur", update);
    country.addEventListener("change", update);
    update();
  });
})();