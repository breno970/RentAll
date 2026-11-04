(() => {
  "use strict";
  const toggle = document.querySelector("[data-nav-toggle]");
  const nav = document.querySelector("#primary-navigation");
  if (toggle && nav) {
    const close = () => { nav.classList.remove("is-open"); toggle.setAttribute("aria-expanded", "false"); toggle.setAttribute("aria-label", "Abrir menu"); toggle.textContent = "☰"; };
    toggle.addEventListener("click", () => {
      const open = !nav.classList.contains("is-open");
      nav.classList.toggle("is-open", open);
      toggle.setAttribute("aria-expanded", String(open));
      toggle.setAttribute("aria-label", open ? "Fechar menu" : "Abrir menu");
      toggle.textContent = open ? "✕" : "☰";
    });
    nav.addEventListener("click", e => { if (e.target.closest("a")) close(); });
    document.addEventListener("keydown", e => { if (e.key === "Escape") close(); });
    document.addEventListener("click", e => { if (!nav.contains(e.target) && !toggle.contains(e.target)) close(); });
    window.addEventListener("resize", () => { if (window.innerWidth > 780) close(); });
  }

  const main = document.querySelector("[data-gallery-main]");
  if (main) {
    document.querySelectorAll("[data-gallery-src]").forEach(btn => {
      btn.addEventListener("click", () => {
        main.src = btn.dataset.gallerySrc;
        main.alt = btn.dataset.galleryAlt || main.alt;
        document.querySelectorAll("[data-gallery-src]").forEach(b => b.setAttribute("aria-pressed", String(b === btn)));
      });
    });
  }

  document.querySelectorAll("[data-category-select]").forEach(select => {
    const target = document.querySelector("[data-type-select]");
    if (!target) return;
    async function load() {
      const id = select.value;
      target.innerHTML = '<option value="">Selecione o tipo</option>';
      target.disabled = !id;
      if (!id) return;
      try {
        const res = await fetch("/Anuncios/GetTipos?id=" + encodeURIComponent(id), {credentials:"same-origin"});
        if (!res.ok) throw new Error("Não foi possível carregar os tipos");
        const items = await res.json();
        for (const item of items) {
          const option = document.createElement("option");
          option.value = item.Value ?? item.value ?? "";
          option.textContent = item.Text ?? item.text ?? "";
          target.appendChild(option);
        }
      } catch {
        target.innerHTML = '<option value="">Falha ao carregar os tipos</option>';
      }
    }
    select.addEventListener("change", load);
  });

  document.querySelectorAll("[data-file-preview]").forEach(el => {
    el.addEventListener("change", () => {
      const target = document.querySelector("#" + el.dataset.filePreview);
      if (target && el.files?.length) target.textContent = el.files.length + " imagem(ns) selecionada(s)";
    });
  });
})();
