(() => {
  "use strict";
  document.addEventListener("DOMContentLoaded", () => {
    const checkout = document.querySelector("[data-demo-checkout]");
    const confirmed = document.querySelector("[data-demo-success]");
    const pay = document.querySelector("[data-demo-pay]");
    const reset = document.querySelector("[data-demo-reset]");
    if (!checkout || !confirmed || !pay || !reset) return;
    pay.addEventListener("click", () => {
      checkout.hidden = true;
      confirmed.hidden = false;
      reset.focus();
    });
    reset.addEventListener("click", () => {
      confirmed.hidden = true;
      checkout.hidden = false;
      pay.focus();
    });
  });
})();