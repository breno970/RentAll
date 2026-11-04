/* RentAll: regiões do Brasil e de Portugal sem dependência obrigatória de API paga. */
(() => {
  "use strict";
  const REGIONS = {
    BR: [
      ["AC","Acre"],["AL","Alagoas"],["AP","Amapá"],["AM","Amazonas"],
      ["BA","Bahia"],["CE","Ceará"],["DF","Distrito Federal"],["ES","Espírito Santo"],
      ["GO","Goiás"],["MA","Maranhão"],["MT","Mato Grosso"],["MS","Mato Grosso do Sul"],
      ["MG","Minas Gerais"],["PA","Pará"],["PB","Paraíba"],["PR","Paraná"],
      ["PE","Pernambuco"],["PI","Piauí"],["RJ","Rio de Janeiro"],["RN","Rio Grande do Norte"],
      ["RS","Rio Grande do Sul"],["RO","Rondônia"],["RR","Roraima"],["SC","Santa Catarina"],
      ["SP","São Paulo"],["SE","Sergipe"],["TO","Tocantins"]
    ],
    PT: [
      ["Aveiro","Aveiro"],["Beja","Beja"],["Braga","Braga"],["Bragança","Bragança"],
      ["Castelo Branco","Castelo Branco"],["Coimbra","Coimbra"],["Évora","Évora"],
      ["Faro","Faro"],["Guarda","Guarda"],["Leiria","Leiria"],["Lisboa","Lisboa"],
      ["Portalegre","Portalegre"],["Porto","Porto"],["Santarém","Santarém"],
      ["Setúbal","Setúbal"],["Viana do Castelo","Viana do Castelo"],
      ["Vila Real","Vila Real"],["Viseu","Viseu"],["Açores","Açores"],["Madeira","Madeira"]
    ]
  };
  const cache = new Map();

  async function citySuggestions(country, region) {
    const key = country + "/" + region;
    if (cache.has(key)) return cache.get(key);
    let url;
    if (country === "BR") {
      url = "https://servicodados.ibge.gov.br/api/v1/localidades/estados/" +
        encodeURIComponent(region) + "/municipios?orderBy=nome";
    } else {
      // GEO API PT disponibiliza municípios por distrito, sem chave de API.
      url = "https://json.geoapi.pt/distrito/" + encodeURIComponent(region.toLowerCase()) + "/municipios";
    }
    const response = await fetch(url, { headers: { Accept: "application/json" } });
    if (!response.ok) throw new Error("Serviço de localização indisponível");
    const result = await response.json();
    const source = Array.isArray(result) ? result
      : Array.isArray(result.municipios) ? result.municipios
      : Array.isArray(result.Municipios) ? result.Municipios : [];
    const names = [...new Set(source.map(x => typeof x === "string" ? x : x.nome || x.nome_municipio || x.name)
      .filter(x => typeof x === "string" && x.length > 0))];
    cache.set(key, names);
    return names;
  }

  function connectCountry(form) {
    const country = form.querySelector("[data-region-country]");
    const region = form.querySelector("[data-region-uf]");
    const city = form.querySelector("[data-region-city]");
    const list = city?.list;
    const postcode = form.querySelector("#txtCep");
    const mapLink = form.querySelector("[data-region-map-link]");
    if (!country || !region) return;
    let requestCounter = 0;

    function updateLocationLink() {
      if (!mapLink) return;
      const location = [city?.value, region.value, country.value === "PT" ? "Portugal" : "Brasil"]
        .filter(Boolean).join(", ");
      if (!city?.value || !region.value) {
        mapLink.setAttribute("aria-disabled", "true");
        mapLink.removeAttribute("href");
        return;
      }
      mapLink.removeAttribute("aria-disabled");
      mapLink.href = "https://www.google.com/maps/search/?api=1&query=" + encodeURIComponent(location);
    }

    async function updateCities() {
      if (!list) return;
      const request = ++requestCounter;
      list.replaceChildren();
      if (!region.value) return;
      list.setAttribute("aria-busy", "true");
      try {
        const names = await citySuggestions(country.value, region.value);
        if (request !== requestCounter) return;
        const fragment = document.createDocumentFragment();
        for (const name of names) fragment.appendChild(new Option(name, name));
        list.replaceChildren(fragment);
      } catch {
        // Entrada manual de cidade/município continua sempre permitida.
      } finally {
        if (request === requestCounter) list.removeAttribute("aria-busy");
      }
    }

    function updateCountry(clearDependentFields) {
      const selectedCountry = country.value === "PT" ? "PT" : "BR";
      const previous = region.value || region.dataset.initialValue || "";
      const options = [new Option(selectedCountry === "PT" ? "Todo o distrito" : "Todo o estado", "")];
      for (const [code, label] of REGIONS[selectedCountry]) options.push(new Option(label, code));
      region.replaceChildren(...options);
      if (!clearDependentFields && REGIONS[selectedCountry].some(x => x[0] === previous))
        region.value = previous;
      if (clearDependentFields) {
        region.value = "";
        if (city) city.value = "";
        if (list) list.replaceChildren();
        if (postcode) postcode.value = "";
      }
      for (const label of form.querySelectorAll("[data-region-label]"))
        label.textContent = (selectedCountry === "PT" ? "Distrito / Região autónoma" : "Estado (UF)") + (label.dataset.required === "true" ? " *" : "");
      for (const label of form.querySelectorAll("[data-postal-label]"))
        label.textContent = (selectedCountry === "PT" ? "Código postal" : "CEP") + (label.dataset.required === "true" ? " *" : " (opcional)");
      for (const label of form.querySelectorAll("[data-rental-currency]"))
        label.textContent = "Preço por dia (" + (selectedCountry === "PT" ? "€" : "R$") + ") (opcional)";
      for (const label of form.querySelectorAll("[data-filter-min-price]"))
        label.textContent = "Preço mínimo por dia (" + (selectedCountry === "PT" ? "€" : "R$") + ")";
      for (const label of form.querySelectorAll("[data-filter-max-price]"))
        label.textContent = "Preço máximo por dia (" + (selectedCountry === "PT" ? "€" : "R$") + ")";
      if (postcode) {
        postcode.placeholder = selectedCountry === "PT" ? "0000-000" : "00000-000";
        postcode.maxLength = selectedCountry === "PT" ? 8 : 9;
        postcode.setCustomValidity("");
      }
      updateLocationLink();
      void updateCities();
    }

    country.addEventListener("change", () => updateCountry(true));
    region.addEventListener("change", () => {
      if (city) city.value = "";
      updateLocationLink();
      void updateCities();
    });
    if (city) {
      city.addEventListener("input", updateLocationLink);
      city.addEventListener("change", updateLocationLink);
    }
    // Preserva a região renderizada pelo ASP.NET no carregamento inicial.
    updateCountry(false);
  }

  function connectCategorySelector() {
    const category = document.querySelector("#Categoria");
    const type = document.querySelector("#Tipo");
    if (!category || !type) return;
    async function refresh() {
      const id = category.value;
      type.replaceChildren(new Option("Selecione o tipo", ""));
      type.disabled = !id;
      if (!id) return;
      try {
        const response = await fetch("/api/v1/catalogo/tipos?categoriaId=" + encodeURIComponent(id), {
          credentials: "same-origin"
        });
        if (!response.ok) throw new Error("Falha ao carregar tipos");
        const types = await response.json();
        for (const row of types) {
          const value = row.id ?? row.value ?? row.Value;
          const label = row.nome ?? row.text ?? row.Text;
          if (value != null && label != null) type.add(new Option(label, value));
        }
        const preferred = type.dataset.selectedType;
        if (preferred && [...type.options].some(option => option.value === preferred)) {
          type.value = preferred;
          delete type.dataset.selectedType;
        }
      } catch {
        type.replaceChildren(new Option("Não foi possível carregar os tipos", ""));
      }
    }
    category.addEventListener("change", refresh);
    if (category.value) void refresh();
  }

  document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll("form:has([data-region-country])").forEach(connectCountry);
    connectCategorySelector();
  });
})();
