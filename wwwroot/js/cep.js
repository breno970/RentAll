/* RentAll — consulta de CEP progressiva: só executa em formulários com o campo. */
async function BuscaCep() {
  const campoCep = document.getElementById("txtCep");
  if (!campoCep) return;
  const cep = (campoCep.value || "").replace(/\D/g, "");
  if (!cep) return;
  if (cep.length !== 8) {
    campoCep.setCustomValidity("Informe um CEP com 8 dígitos.");
    campoCep.reportValidity();
    return;
  }
  campoCep.setCustomValidity("");
  campoCep.setAttribute("aria-busy", "true");
  try {
    const resposta = await fetch("https://brasilapi.com.br/api/cep/v1/" + encodeURIComponent(cep));
    if (!resposta.ok) throw new Error(resposta.status === 404 ? "CEP não encontrado." : "Falha ao consultar o CEP.");
    const endereco = await resposta.json();
    for (const [id, value] of Object.entries({
      txtRua: endereco.street,
      txtBairro: endereco.neighborhood,
      txtCidade: endereco.city,
      txtEstado: endereco.state
    })) {
      const field = document.getElementById(id);
      if (field && typeof value === "string" && value) field.value = value;
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
