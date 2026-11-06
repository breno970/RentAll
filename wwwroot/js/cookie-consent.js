/* RentAll consent choices — no advertising or analytics are loaded by this code. */
(() => {
  "use strict";
  const KEY = "rentall_cookie_choice_v1";
  const VERSION = 1;
  const panel = document.getElementById("ra-cookie-consent");
  const prefs = document.getElementById("ra-cookie-preferences");
  const external = document.getElementById("ra-cookie-external");
  const status = document.getElementById("ra-cookie-status");
  let current = null;
  let resourcesLoaded = false;

  function stored() {
    try {
      const value = JSON.parse(localStorage.getItem(KEY) || "null");
      if (value && value.version === VERSION && typeof value.external === "boolean") return value;
    } catch { /* preference storage may be unavailable */ }
    return null;
  }
  function loadExternal() {
    if (resourcesLoaded) return;
    resourcesLoaded = true;
    const link = document.createElement("link");
    link.rel = "stylesheet";
    link.href = "https://fonts.googleapis.com/css2?family=DM+Sans:wght@400;500;600;700&family=Plus+Jakarta+Sans:wght@400;500;600;700;800&display=swap";
    document.head.appendChild(link);
    const icons = document.createElement("script");
    icons.defer = true;
    icons.crossOrigin = "anonymous";
    icons.src = "https://kit.fontawesome.com/f9a4013b2c.js";
    document.head.appendChild(icons);
  }
  function apply(choice) {
    current = choice;
    if (choice.external) loadExternal();
    document.documentElement.dataset.externalResources = choice.external ? "on" : "off";
    if (panel) panel.hidden = true;
    if (prefs) prefs.hidden = true;
    if (status) status.textContent = "Preferências guardadas. Podes alterá-las no rodapé.";
    document.dispatchEvent(new CustomEvent("rentall:consent-change", { detail: { external: choice.external } }));
  }
  function save(externalAllowed) {
    const wasExternal = Boolean(current?.external);
    const choice = { version: VERSION, external: Boolean(externalAllowed), updatedAt: new Date().toISOString() };
    try { localStorage.setItem(KEY, JSON.stringify(choice)); } catch { /* keep this tab's decision */ }
    apply(choice);
    // Revocation must stop any already-loaded external kit resources.
    if (wasExternal && !choice.external) window.location.reload();
  }
  function showSettings() {
    if (!panel) return;
    panel.hidden = false;
    if (prefs) prefs.hidden = false;
    const saveButton = panel.querySelector("[data-cookie-save]");
    const manageButton = panel.querySelector("[data-cookie-manage]");
    if (saveButton) saveButton.hidden = false;
    if (manageButton) manageButton.hidden = true;
    if (external) external.checked = Boolean(current?.external);
    panel.querySelector("[data-cookie-save]")?.focus();
  }

  document.addEventListener("DOMContentLoaded", () => {
    const choice = stored();
    if (choice) apply(choice);
    else {
      document.documentElement.dataset.externalResources = "off";
      if (panel) panel.hidden = false;
    }
    document.querySelectorAll("[data-cookie-reopen]").forEach(button => button.addEventListener("click", showSettings));
    document.querySelector("[data-cookie-accept]")?.addEventListener("click", () => save(true));
    document.querySelector("[data-cookie-reject]")?.addEventListener("click", () => save(false));
    document.querySelector("[data-cookie-manage]")?.addEventListener("click", showSettings);
    document.querySelector("[data-cookie-save]")?.addEventListener("click", () => save(Boolean(external?.checked)));
    document.querySelector("[data-cookie-close]")?.addEventListener("click", () => {
      if (current) { panel.hidden = true; return; }
      // Before a decision is made, closing is equivalent to rejecting optional resources.
      save(false);
    });
  });
  window.rentallCookies = Object.freeze({
    canLoadExternal: () => current?.external === true,
    openSettings: showSettings
  });
})();