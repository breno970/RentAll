(() => {
  "use strict";

  document.addEventListener("DOMContentLoaded", () => {
    const editor = document.getElementById("messageText");
    if (!editor) return;

    const form = editor.closest("[data-guarded-message-form]");
    const warning = form?.querySelector("[data-message-warning]");

    // Browser hints reduce errors; the ASP.NET controller independently enforces
    // the full contact policy. This code must not be treated as a security boundary.
    function looksExternal(value) {
      const text = String(value || "")
        .normalize("NFKC")
        .replace(/[\u200B-\u200D\uFEFF]/g, "")
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "");

      const obvious = /(?:https?:\/\/|www\.|\b(?:instagram|whats\s*app|whats|telegram|facebook|snapchat|signal|discord|tiktok|mb\s*way|pix|iban|revolut|paypal)\b|@[a-z][a-z0-9._]{2,}|[a-z0-9._+-]+@[a-z0-9.-]+\.[a-z]{2,})/i;
      const phone = /(?:\d[\s().\-_/|,]*){9,15}/;
      const website = /\b[a-z0-9-]+\s*(?:\.|\[ponto\]|\s+ponto\s+)\s*(?:com|pt|br|org|net|io|me)\b/i;
      return obvious.test(text) || phone.test(text) || website.test(text);
    }

    function showWarning(visible) {
      if (!warning) return;
      warning.hidden = !visible;
      warning.textContent = visible
        ? "Esta mensagem parece incluir um contacto ou pagamento externo. Retira essa informação e combina o aluguer aqui."
        : "";
    }

    editor.addEventListener("input", () => showWarning(false));
    form?.addEventListener("submit", event => {
      if (!looksExternal(editor.value)) return;
      event.preventDefault();
      showWarning(true);
      editor.focus();
    });

    document.querySelectorAll("[data-chat-suggestion]").forEach(button => {
      button.addEventListener("click", () => {
        const proposal = button.getAttribute("data-chat-suggestion") || "";
        if (!proposal) return;
        editor.value = editor.value.trim()
          ? editor.value.trim() + "\n" + proposal
          : proposal;
        editor.focus();
        editor.setSelectionRange(editor.value.length, editor.value.length);
      });
    });
  });
})();
