// Load Google Maps only after a real user action to reduce third-party tracking.
(() => {
  "use strict";
  let loading;
  window.rentallLoadGoogleMaps = () => {
    if (window.google?.maps?.Geocoder) {
      window.rentallGoogleMapsReady?.();
      return Promise.resolve();
    }
    if (loading) return loading;
    const key = document.querySelector('meta[name="rentall-maps-browser-key"]')?.content;
    if (!key) return Promise.reject(new Error("A chave Google Maps não está configurada no servidor."));
    loading = new Promise((resolve, reject) => {
      const callback = window.rentallGoogleMapsReady;
      window.rentallGoogleMapsReady = () => {
        try { callback?.(); resolve(); }
        catch (error) { reject(error); }
      };
      const script = document.createElement("script");
      script.async = true;
      script.src = "https://maps.googleapis.com/maps/api/js?key=" + encodeURIComponent(key)
        + "&loading=async&callback=rentallGoogleMapsReady";
      script.onerror = () => { loading = null; reject(new Error("Não foi possível carregar o Google Maps.")); };
      document.head.appendChild(script);
    });
    return loading;
  };
  document.addEventListener("click", async event => {
    const button = event.target.closest("[data-load-approx-map]");
    if (!button) return;
    const target = button.parentElement?.querySelector("[data-ra-map]");
    const status = button.parentElement?.querySelector("[data-inline-map-status]");
    button.disabled = true;
    try {
      await window.rentallLoadGoogleMaps();
      if (target) target.hidden = false;
      if (status) status.textContent = "O mapa mostra apenas uma zona aproximada.";
    } catch (error) {
      if (status) status.textContent = error.message || "Não foi possível abrir o mapa.";
    } finally { button.disabled = false; }
  });
})();