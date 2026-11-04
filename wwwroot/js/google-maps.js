/* Google Maps é opcional e só liga com uma chave de browser restrita no Google Cloud.
   Os links do Google Maps continuam disponíveis sem esta API. */
window.rentallGoogleMapsReady = function () {
  if (!window.google?.maps) return;
  const maps = document.querySelectorAll("[data-ra-map]");
  if (!maps.length) return;

  for (const target of maps) {
    const form = target.closest("form") || document;
    const state = form.querySelector("[data-region-uf]");
    const city = form.querySelector("[data-region-city]");
    const staticPlace = target.dataset.place || "";
    const map = new google.maps.Map(target, {
      center: { lat: -14.235, lng: -51.9253 },
      zoom: 4, mapTypeControl: false, streetViewControl: false,
      fullscreenControl: false, gestureHandling: "cooperative"
    });
    const geocoder = new google.maps.Geocoder();
    let ring = null;
    let scheduled = null;
    let current = 0;
    async function update() {
      const place = staticPlace || [city?.value, state?.value, "Brasil"].filter(Boolean).join(", ");
      if (!place || (!staticPlace && (!city?.value || !state?.value))) return;
      const request = ++current;
      try {
        const result = await geocoder.geocode({ address: place, region: "br" });
        if (request !== current || !result.results.length) return;
        const point = result.results[0].geometry.location;
        map.setCenter(point);
        map.setZoom(11);
        if (ring) ring.setMap(null);
        ring = new google.maps.Circle({
          map, center: point, radius: 3000,
          strokeColor: "#2579b7", fillColor: "#2579b7",
          strokeOpacity: .8, fillOpacity: .16, strokeWeight: 2
        });
      } catch {
        // O link de pesquisa do Google Maps permanece o método de consulta alternativo.
      }
    }
    function schedule() {
      if (scheduled) clearTimeout(scheduled);
      scheduled = setTimeout(update, 700);
    }
    target.hidden = false;
    if (staticPlace) void update();
    if (state) state.addEventListener("change", schedule);
    if (city) city.addEventListener("change", schedule);
    if (city) city.addEventListener("blur", schedule);
  }
};
