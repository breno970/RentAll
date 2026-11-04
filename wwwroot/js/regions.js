/* RentAll: estados e municípios do IBGE, com introdução manual caso a API falhe. */
(() => {
  "use strict";
  const municipalityCache = new Map();

  async function citiesForUf(uf) {
    if (municipalityCache.has(uf)) return municipalityCache.get(uf);
    const url = "https://servicodados.ibge.gov.br/api/v1/localidades/estados/" +
      encodeURIComponent(uf) + "/municipios?orderBy=nome";
    const response = await fetch(url, { headers: { Accept: "application/json" } });
    if (!response.ok) throw new Error("Não foi possível consultar os municípios.");
    const data = await response.json();
    const names = Array.isArray(data) ? data.map(city => city.nome).filter(Boolean) : [];
    municipalityCache.set(uf, names);
    return names;
  }

  function connect(ufSelect) {
    const scope = ufSelect.closest("form") || document;
    const cityInput = scope.querySelector("[data-region-city]");
    if (!cityInput) return;
    const list = cityInput.list;
    if (!list) return;

    let currentRequest = 0;
    async function refresh() {
      const request = ++currentRequest;
      const uf = ufSelect.value;
      list.replaceChildren();
      if (!/^[A-Z]{2}$/.test(uf)) return;
      list.setAttribute("aria-busy", "true");
      try {
        const cities = await citiesForUf(uf);
        if (request !== currentRequest) return;
        const fragment = document.createDocumentFragment();
        for (const name of cities) {
          const option = document.createElement("option");
          option.value = name;
          fragment.appendChild(option);
        }
        list.replaceChildren(fragment);
      } catch {
        // A seleção manual de cidade continua disponível sem internet/IBGE.
      } finally {
        if (request === currentRequest) list.removeAttribute("aria-busy");
      }
    }

    ufSelect.addEventListener("change", () => {
      if (ufSelect.dataset.clearCityOnChange !== "false") cityInput.value = "";
      void refresh();
      updateMapLink(scope);
    });
    cityInput.addEventListener("input", () => updateMapLink(scope));
    if (ufSelect.value) void refresh();
    updateMapLink(scope);
  }

  function updateMapLink(scope) {
    const state = scope.querySelector("[data-region-uf]")?.value || "";
    const city = scope.querySelector("[data-region-city]")?.value || "";
    const link = scope.querySelector("[data-region-map-link]");
    if (!link) return;
    if (!state && !city) { link.setAttribute("aria-disabled", "true"); return; }
    link.removeAttribute("aria-disabled");
    const query = [city, state, "Brasil"].filter(Boolean).join(", ");
    link.href = "https://www.google.com/maps/search/?api=1&query=" + encodeURIComponent(query);
  }

  document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll("[data-region-uf]").forEach(connect);
    const category = document.querySelector("#Categoria");
    const type = document.querySelector("#Tipo");
    if (category && type) {
      async function loadTypes() {
        const id = category.value;
        type.replaceChildren(new Option("Selecione um tipo", ""));
        type.disabled = !id;
        if (!id) return;
        try {
          const response = await fetch("/Anuncios/GetTipos?id=" + encodeURIComponent(id), { credentials: "same-origin" });
          if (!response.ok) throw new Error("Falha ao carregar tipos");
          const types = await response.json();
          for (const row of types) {
            const value = row.value ?? row.Value;
            const label = row.text ?? row.Text;
            if (value != null && label != null) type.add(new Option(label, value));
          }
        } catch {
          type.replaceChildren(new Option("Não foi possível carregar os tipos", ""));
        }
      }
      category.addEventListener("change", loadTypes);
      if (category.value) void loadTypes();
    }
  });
})();
