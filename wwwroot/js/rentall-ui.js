(() => {
  "use strict";
  document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll("img[data-photo-fallback]").forEach(image => {
      const note = image.parentElement.querySelector("[data-photo-note]");
      const fallback = new URL(image.dataset.photoFallback, document.baseURI).href;
      const showFallback = () => {
        if (image.src === fallback) return;
        image.src = fallback;
        image.alt = "";
        if (note) note.hidden = false;
      };
      image.addEventListener("error", showFallback);
      if (image.getAttribute("src") && image.complete && !image.naturalWidth) showFallback();
    });
    const button = document.querySelector("[data-menu-toggle]");
    const menu = document.getElementById("main-navigation");
    if (button && menu) {
      const setOpen = (open) => {
        button.setAttribute("aria-expanded", String(open));
        button.setAttribute("aria-label", open ? "Fechar menu" : "Abrir menu");
        menu.classList.toggle("is-open", open);
        document.body.classList.toggle("menu-open", open);
      };
      button.addEventListener("click", () => setOpen(button.getAttribute("aria-expanded") !== "true"));
      menu.addEventListener("click", (e) => { if (e.target.closest("a")) setOpen(false); });
      document.addEventListener("keydown", e => {
        if (e.key === "Escape" && menu.classList.contains("is-open")) { setOpen(false); button.focus(); }
      });
      document.addEventListener("pointerdown", e => {
        if (!button.contains(e.target) && !menu.contains(e.target)) setOpen(false);
      });
      window.addEventListener("resize", () => { if (window.innerWidth > 1040) setOpen(false); });
    }
    document.querySelectorAll("[data-dismiss-parent]").forEach(button => {
      button.addEventListener("click", () => button.closest(".notice")?.remove());
    });
    const gallery = document.querySelector("[data-gallery-main]");
    if (gallery) {
      document.querySelectorAll("[data-gallery-image]").forEach(button => {
        button.addEventListener("click", () => {
          gallery.src = button.dataset.galleryImage;
          gallery.alt = button.dataset.galleryAlt || gallery.alt;
          document.querySelectorAll("[data-gallery-image]").forEach(b => b.setAttribute("aria-pressed", b === button ? "true" : "false"));
        });
      });
    }
    const files = document.querySelectorAll("[data-file-input]");
    files.forEach(input => {
      const target = document.querySelector("#" + input.dataset.fileInput);
      if (!target) return;
      input.addEventListener("change", () => {
        const file = input.files?.[0];
        if (!file) { target.replaceChildren(); return; }
        if (!/^image\/(jpeg|png|webp)$/.test(file.type) || file.size > 5*1024*1024) {
          input.value = "";
          target.textContent = "Escolhe uma imagem JPG, PNG ou WebP de até 5 MB.";
          target.classList.add("is-invalid");
          return;
        }
        const url = URL.createObjectURL(file);
        const img = document.createElement("img");
        img.alt = "Pré-visualização de " + file.name;
        img.onload = () => URL.revokeObjectURL(url);
        img.src = url;
        target.classList.remove("is-invalid");
        target.replaceChildren(img);
      });
    });
    document.querySelectorAll("[data-toggle-password]").forEach(button => {
      button.addEventListener("click", () => {
        const input = document.getElementById(button.dataset.togglePassword);
        if (!input) return;
        input.type = input.type === "password" ? "text" : "password";
        button.setAttribute("aria-label", input.type === "password" ? "Mostrar palavra-passe" : "Ocultar palavra-passe");
        button.setAttribute("aria-pressed", String(input.type === "text"));
        const icon = button.querySelector("i");
        if (icon) { icon.classList.toggle("fa-eye", input.type === "password"); icon.classList.toggle("fa-eye-slash", input.type !== "password"); }
      });
    });
    document.querySelectorAll("[data-submit-lock]").forEach(form => {
      form.addEventListener("submit", event => {
        if (event.defaultPrevented || !form.checkValidity()) return;
        // A second click while the first request is in flight would repeat the operation.
        if (form.dataset.submitting === "true") { event.preventDefault(); return; }
        form.dataset.submitting = "true";
        const btn = event.submitter || form.querySelector("button[type=submit]");
        if (btn) {
          btn.setAttribute("aria-busy", "true");
          btn.classList.add("is-loading");
        }
        // Do not disable submit; the browser must still send the clicked button's name/value.
        // Pages restored from the back/forward cache must be usable again.
        window.addEventListener("pageshow", () => {
          delete form.dataset.submitting;
          btn?.removeAttribute("aria-busy");
          btn?.classList.remove("is-loading");
        }, { once: true });
      });
    });
    document.querySelectorAll("[data-print-page]").forEach(button => {
      button.addEventListener("click", () => window.print());
    });
  });
})();
