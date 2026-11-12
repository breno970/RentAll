(() => {
  "use strict";

  const root = document.querySelector("[data-quick-demo]");
  if (!root) return;

  const panels = [...root.querySelectorAll("[data-quick-panel]")];
  const steps = [...root.querySelectorAll("[data-quick-step]")];
  const form = panels[0];
  const days = root.querySelector("[data-quick-days]");
  const totals = root.querySelectorAll("[data-quick-total]");
  const duration = root.querySelector("[data-quick-duration]");
  const rate = Number(root.dataset.rateCentimos);
  const money = new Intl.NumberFormat("pt-PT", { style: "currency", currency: "EUR" });

  function updateQuote() {
    const count = days.valueAsNumber;
    const valid = days.validity.valid;
    const total = valid ? money.format(rate * count / 100) : "—";
    totals.forEach(output => { output.textContent = total; });
    duration.textContent = valid ? (count === 1 ? "1 dia" : `${count} dias`) : "";
  }

  function showPanel(index) {
    panels.forEach((panel, position) => { panel.hidden = index !== position; });
    steps.forEach((step, position) => {
      if (position === index) step.setAttribute("aria-current", "step");
      else step.removeAttribute("aria-current");
      step.classList.toggle("is-complete", position < index);
    });
    panels[index].querySelector("h2").focus();
  }

  days.addEventListener("input", updateQuote);
  form.addEventListener("submit", event => {
    event.preventDefault();
    if (!form.reportValidity()) return;
    updateQuote();
    showPanel(1);
  });
  panels[1].querySelector("[data-quick-next]").addEventListener("click", () => showPanel(2));
  panels[2].querySelector("[data-quick-next]").addEventListener("click", () => showPanel(3));
  root.querySelector("[data-quick-restart]").addEventListener("click", () => {
    form.reset();
    updateQuote();
    showPanel(0);
  });
  updateQuote();
})();
