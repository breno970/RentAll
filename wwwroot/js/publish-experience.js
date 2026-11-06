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

    // Free reverse geocoding with explicit browser permission. No server proxy,
    // no saved coordinates, no silent IP geolocation.
    button?.addEventListener("click", () => {
      if (!navigator.geolocation) {
        show("Este navegador não suporta localização. Seleciona a cidade manualmente.");
        return;
      }
      button.disabled = true;
      show("A pedir autorização para identificar a tua cidade...");
      navigator.geolocation.getCurrentPosition(async position => {
        try {
          const params = new URLSearchParams({
            latitude: String(position.coords.latitude),
            longitude: String(position.coords.longitude),
            localityLanguage: "pt"
          });
          const response = await fetch("https://api.bigdatacloud.net/data/reverse-geocode-client?" + params, {
            method: "GET", mode: "cors", cache: "no-store",
            credentials: "omit", referrerPolicy: "no-referrer"
          });
          if (!response.ok) throw new Error("O serviço gratuito de localização está temporariamente indisponível.");
          const result = await response.json();
          const code = String(result.countryCode || "").toUpperCase();
          if (code !== "PT" && code !== "BR") {
            show("A RentAll opera em Portugal e no Brasil. Seleciona a zona manualmente.");
            return;
          }

          // regions.js repopulates the select synchronously when country changes.
          if (country.value !== code) {
            country.value = code;
            country.dispatchEvent(new Event("change", { bubbles: true }));
          }
          const ascii = value => String(value || "").normalize("NFD")
            .replace(/[\u0300-\u036f]/g, "").toLowerCase().trim();
          const iso = String(result.principalSubdivisionCode || "");
          const candidates = [iso, iso.slice(3), result.principalSubdivision];
          const match = [...region.options].find(option => candidates.some(candidate =>
            ascii(candidate) === ascii(option.value) || ascii(candidate) === ascii(option.text)));
          if (match) {
            region.value = match.value;
            region.dispatchEvent(new Event("change", { bubbles: true }));
          }
          // Return only municipal-level fields to the form. Do not use locality,
          // street, postcode, GPS precision, or nearest-address data.
          const municipality = String(result.city || "").trim();
          if (municipality) {
            city.value = municipality.slice(0, 120);
            city.dispatchEvent(new Event("input", { bubbles: true }));
          }
          if (neighborhood) neighborhood.value = "";
          show(match && municipality
            ? "Encontrámos a tua zona aproximada. Confirma o distrito e a cidade."
            : "Identificámos o país. Completa o distrito e a cidade manualmente.");
        } catch (error) {
          show(error.message || "Não foi possível identificar a zona. Podes introduzir a cidade manualmente.");
        } finally {
          button.disabled = false;
        }
      }, error => {
        button.disabled = false;
        show(error.code === 1
          ? "Não deste autorização para usar a localização. Podes selecionar a cidade manualmente."
          : "Não conseguimos obter a posição do dispositivo. Verifica as permissões ou escolhe a cidade manualmente.");
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