(() => {
  "use strict";
  document.addEventListener("DOMContentLoaded", () => {
    const editor = document.querySelector("#messageText");
    if (!editor) return;
    const form = document.querySelector("[data-guarded-message-form]");
    const warning = form?.querySelector("[data-message-warning]");
    const hasContact = value => {
      const text = String(value || "").normalize("NFKC")
        .replace(/[\\u200B-\\u200D\\uFEFF]/g, "")
        .normalize("NFD").replace(/[\\u0300-\\u036f]/g, "");
      return /(?:https?:\\/\\/|www\\.|\\b(?:instagram|whats\\s*app|whats|telegram|facebook|snapchat|signal|discord|tiktok|mb\\s*way|pix|iban|revolut|paypal)\\b|@[a-z][a-z0-9._]{2,}|[a-z0-9._+-]+@[a-z0-9.-]+\\.[a-z]{2,})/i.test(text) ||
        /(?:\\d[\\s().\\-_/|,]*){9,15}/.test(text) ||
        /\\b[a-z0-9-]+\\s*(?:\\.|\\[ponto\\]|\\s+ponto\\s+)\\s*(?:com|pt|br|org|net|io|me)\\b/i.test(text);
    };
    function warn(value) {
      if (!warning) return;
      warning.hidden = !value;
      warning.textContent = value ? "Esta mensagem parece incluir um contacto ou pagamento externo. Retira essa informação para poderes enviar. Podes combinar o aluguer aqui." : "";
    }
    editor.addEventListener("input", () => warn(false));
    form?.addEventListener("submit", event => {
      if (!hasContact(editor.value)) return;
      event.preventDefault();
      warn(true);
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
