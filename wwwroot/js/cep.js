/* RentAll — consulta de CEP progressiva: só executa em formulários com o campo. */
async function BuscaCep() {
  const campoCep = document.getElementById("txtCep");
  if (!campoCep) return;
  const cep = (campoCep.value || "").replace(/\D/g, "");
  const country = campoCep.closest("form")?.querySelector("[data-region-country]")?.value || "BR";
  if (!cep) return;
  if (country === "PT") {
    if (cep.length !== 7) {
      campoCep.setCustomValidity("Informe um código postal português com 7 dígitos (0000-000).");
      campoCep.reportValidity();
    } else {
      campoCep.setCustomValidity("");
      campoCep.value = cep.slice(0, 4) + "-" + cep.slice(4);
    }
    // O município e o distrito são preenchidos manualmente com sugestões da GEO API PT.
    return;
  }
  if (cep.length !== 8) {
    campoCep.setCustomValidity("Informe um CEP brasileiro com 8 dígitos.");
    campoCep.reportValidity();
    return;
  }
  campoCep.value = cep.slice(0, 5) + "-" + cep.slice(5);
  campoCep.setCustomValidity("");
  campoCep.setAttribute("aria-busy", "true");
  try {
    const resposta = await fetch("https://brasilapi.com.br/api/cep/v1/" + encodeURIComponent(cep));
    if (!resposta.ok) throw new Error(resposta.status === 404 ? "CEP não encontrado." : "Falha ao consultar o CEP.");
    const endereco = await resposta.json();
    // Estado deve ser preenchido antes da cidade para o seletor do IBGE
    // não limpar o município automaticamente selecionado via CEP.
    const uf = document.getElementById("txtEstado");
    if (uf && typeof endereco.state === "string" && endereco.state) {
      uf.value = endereco.state.toUpperCase();
      uf.dispatchEvent(new Event("change", { bubbles: true }));
    }
    for (const [id, value] of Object.entries({
      txtCidade: endereco.city,
      txtBairro: endereco.neighborhood,
      txtRua: endereco.street
    })) {
      const field = document.getElementById(id);
      if (field && typeof value === "string" && value) {
        field.value = value;
        field.dispatchEvent(new Event("input", { bubbles: true }));
      }
    }
  } catch (error) {
    campoCep.setCustomValidity(error.message || "Não foi possível consultar o CEP.");
    campoCep.reportValidity();
  } finally {
    campoCep.removeAttribute("aria-busy");
  }
}
document.addEventListener("DOMContentLoaded", () => {
  const campoCep = document.getElementById("txtCep");
  if (!campoCep || campoCep.dataset.raCepBound === "true") return;
  campoCep.dataset.raCepBound = "true";
  campoCep.addEventListener("blur", BuscaCep);
  campoCep.addEventListener("input", () => campoCep.setCustomValidity(""));
});
