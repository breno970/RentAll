(() => {
  "use strict";
  document.addEventListener("DOMContentLoaded", () => {
    const editor = document.querySelector("#messageText");
    if (!editor) return;
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
