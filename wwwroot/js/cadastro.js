/* RentAll — mantém o ícone original da lua/sol e evita erros noutras páginas. */
(() => {
  const mode = document.getElementById("mode_icon");
  const form = document.getElementById("login_form");
  if (mode && form) {
    const toggleTheme = () => {
      const dark = !form.classList.contains("dark");
      form.classList.toggle("dark", dark);
      mode.classList.toggle("fa-moon", !dark);
      mode.classList.toggle("fa-sun", dark);
    };
    mode.setAttribute("role", "button");
    mode.setAttribute("tabindex", "0");
    mode.setAttribute("aria-label", "Alternar aparência do formulário");
    mode.addEventListener("click", toggleTheme);
    mode.addEventListener("keydown", event => {
      if (event.key === "Enter" || event.key === " ") {
        event.preventDefault();
        toggleTheme();
      }
    });
  }
  const subir = document.getElementById("subir");
  if (subir) {
    subir.addEventListener("click", event => {
      event.preventDefault();
      window.scrollTo({ top: 0, behavior: window.matchMedia("(prefers-reduced-motion: reduce)").matches ? "auto" : "smooth" });
    });
  }
})();
