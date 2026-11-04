(() => {
  "use strict";
  document.addEventListener("DOMContentLoaded", () => {
    const form = document.querySelector("[data-single-publish]");
    if (!form) return;
    const country = form.querySelector("[data-region-country]");
    const region = form.querySelector("[data-region-uf]");
    const city = form.querySelector("[data-region-city]");
    const neighborhood = form.querySelector("#Bairro");
    const button = form.querySelector("[data-use-approx-location]");
    const status = form.querySelector("[data-location-feedback]");
    const price = form.querySelector("[data-money-input]");
    const symbol = form.querySelector("[data-money-symbol]");
    const title = form.querySelector("[data-smart-title]");
    const category = form.querySelector("#Categoria");
    const description = form.querySelector("[data-smart-description]");

    function show(message) { status.textContent = message; }
    function asNumber(text) {
      const raw = String(text || "").replace(/[^0-9.,]/g, "");
      const comma = raw.lastIndexOf(",");
      const dot = raw.lastIndexOf(".");
      const normalized = comma !== -1 && dot !== -1
        ? (comma > dot ? raw.replace(/\./g, "").replace(",", ".") : raw.replace(/,/g, ""))
        : raw.replace(",", ".");
      return Number(normalized);
    }
    const currency = () => country?.value === "BR" ? "BRL" : "EUR";
    const locale = () => country?.value === "BR" ? "pt-BR" : "pt-PT";
    function updateCurrency() {
      symbol.textContent = currency() === "BRL" ? "R$" : "€";
      if (price.value.trim()) {
        const n = asNumber(price.value);
        if (Number.isFinite(n) && n >= 0) price.value = new Intl.NumberFormat(locale(), { minimumFractionDigits:2, maximumFractionDigits:2 }).format(n);
      }
    }
    country?.addEventListener("change", updateCurrency);
    price?.addEventListener("blur", updateCurrency);
    price?.addEventListener("focus", () => { if (price.value.trim()) price.value = String(asNumber(price.value)).replace(".", ","); });
    updateCurrency();

    button?.addEventListener("click", () => {
      if (!navigator.geolocation) { show("O navegador não suporta a deteção de localização. Escolhe a zona manualmente."); return; }
      if (!window.google?.maps?.Geocoder) {
        show("A identificação automática precisa da API Google Maps configurada. Podes selecionar o distrito e a cidade manualmente.");
        return;
      }
      button.disabled = true;
      show("A pedir permissão para obter a tua zona...");
      navigator.geolocation.getCurrentPosition(async position => {
        try {
          // Only used transiently for reverse geocoding; NEVER stored or added to the form.
          const geocoder = new google.maps.Geocoder();
          const { results } = await geocoder.geocode({
            location: { lat: position.coords.latitude, lng: position.coords.longitude }
          });
          const components = results.flatMap(result => result.address_components || []);
          const find = type => components.find(comp => comp.types.includes(type));
          const code = find("country")?.short_name;
          if (code !== "PT" && code !== "BR") {
            show("A RentAll está disponível apenas em Portugal e no Brasil. Seleciona a zona manualmente.");
            return;
          }
          country.value = code;
          country.dispatchEvent(new Event("change", { bubbles: true }));
          const regionPart = find("administrative_area_level_1");
          const candidates = [regionPart?.short_name, regionPart?.long_name].filter(Boolean);
          const found = [...region.options].find(option => candidates.some(name =>
            option.value.toLocaleLowerCase("pt") === name.toLocaleLowerCase("pt") ||
            option.text.toLocaleLowerCase("pt") === name.toLocaleLowerCase("pt")));
          if (found) {
            region.value = found.value;
            region.dispatchEvent(new Event("change", { bubbles: true }));
          }
          const municipality = find("locality")?.long_name || find("administrative_area_level_2")?.long_name
            || find("postal_town")?.long_name;
          if (municipality) {
            city.value = municipality;
            city.dispatchEvent(new Event("input", { bubbles: true }));
          }
          if (neighborhood) neighborhood.value = "";
          show(found && municipality
            ? "Zona aproximada identificada. Confirma o distrito e a cidade antes de publicar."
            : "Localização recebida. Confirma e completa manualmente a região e a cidade.");
        } catch {
          show("O Google Maps não conseguiu identificar a zona. Seleciona a cidade manualmente.");
        } finally { button.disabled = false; }
      }, error => {
        button.disabled = false;
        show(error.code === 1 ? "Permissão recusada. Podes escolher a zona manualmente."
          : "Não foi possível obter a localização. Verifica o GPS ou escolhe a zona manualmente.");
      }, { enableHighAccuracy: false, maximumAge: 300000, timeout: 12000 });
    });

    // Smart hints are deterministic, local and privacy-friendly: no text leaves the browser.
    const keywords = [
      ["Ferramentas", /furadeira|berbequim|martelo|serra|parafusadora|broca|andaime|betoneira/i],
      ["Fotografia", /câmara|camera|lente|objetiva|tripé|drone|fotograf/i],
      ["Desporto", /bicicleta|surf|prancha|patins|ténis|tenis|ginásio|academia/i],
      ["Música", /guitarra|violão|piano|teclado|bateria|microfone|amplificador/i],
      ["Eventos", /festa|casamento|mesa|cadeira|som|luz|tenda|projetor/i],
      ["Tecnologia", /computador|portátil|notebook|tablet|monitor|consola|videogame/i]
    ];
    title?.addEventListener("blur", () => {
      if (category?.value || !title.value.trim()) return;
      const suggested = keywords.find(([, regex]) => regex.test(title.value));
      if (!suggested) return;
      const categoryOption = [...category.options].find(o =>
        o.text.toLocaleLowerCase("pt").includes(suggested[0].toLocaleLowerCase("pt")));
      if (categoryOption) {
        category.value = categoryOption.value;
        category.dispatchEvent(new Event("change", { bubbles: true }));
      }
    });
    form.querySelector("[data-description-assist]")?.addEventListener("click", () => {
      if (!description || description.value.trim()) return;
      const subject = title?.value.trim() || "Este artigo";
      description.value = subject + " disponível para aluguer.\nEstado de conservação: \nInclui: \nCondições de utilização: ";
      description.focus();
    });
  });
})();