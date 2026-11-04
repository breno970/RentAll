(() => {
  "use strict";
  const MAX_FILES = 5;
  const MAX_BYTES = 5 * 1024 * 1024;
  const TYPES = ["image/jpeg", "image/png", "image/webp"];
  const clean = e => { if (e.url && e.file) URL.revokeObjectURL(e.url); };
  async function optimize(file) {
    if (!TYPES.includes(file.type)) throw new Error("Formato não suportado. Usa fotografias JPG, PNG ou WebP.");
    if (!file.size) throw new Error("A fotografia está vazia.");
    if (file.size > 35 * 1024 * 1024) throw new Error("Esta fotografia é demasiado grande. Escolhe uma de até 35 MB.");
    const url = URL.createObjectURL(file);
    try {
      const img = new Image();
      img.src = url;
      await img.decode();
      const scale = Math.min(1, 1800 / Math.max(img.naturalWidth, img.naturalHeight));
      if (file.size < 1.5 * 1024 * 1024 && scale === 1) return file;
      const canvas = document.createElement("canvas");
      canvas.width = Math.max(1, Math.round(img.naturalWidth * scale));
      canvas.height = Math.max(1, Math.round(img.naturalHeight * scale));
      const ctx = canvas.getContext("2d");
      if (!ctx) throw new Error("Não foi possível processar esta fotografia.");
      ctx.fillStyle = "#ffffff"; ctx.fillRect(0, 0, canvas.width, canvas.height);
      ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
      const blob = await new Promise(resolve => canvas.toBlob(resolve, "image/jpeg", .78));
      if (!blob) throw new Error("Não foi possível otimizar a fotografia.");
      const output = new File([blob], file.name.replace(/\.[^.]+$/, "") + ".jpg", { type: "image/jpeg" });
      return output.size < file.size ? output : file;
    } catch (error) {
      // If the browser cannot decode an otherwise valid file, allow upload only when small enough.
      if (file.size <= MAX_BYTES) return file;
      throw new Error("Não foi possível reduzir esta fotografia. Converte-a para JPG e tenta novamente.");
    } finally { URL.revokeObjectURL(url); }
  }
  document.querySelectorAll("[data-photo-carousel-editor]").forEach(root => {
    const form = root.closest("form");
    const fileInput = root.querySelector("[data-photo-files]");
    const orderInput = root.querySelector("[data-photo-order]");
    const image = root.querySelector("[data-photo-preview]");
    const placeholder = root.querySelector("[data-photo-empty]");
    const status = root.querySelector("[data-photo-status]");
    const counter = root.querySelector("[data-photo-counter]");
    const prev = root.querySelector("[data-photo-prev]");
    const next = root.querySelector("[data-photo-next]");
    const remove = root.querySelector("[data-photo-remove]");
    const left = root.querySelector("[data-photo-left]");
    const right = root.querySelector("[data-photo-right]");
    const cover = root.querySelector("[data-photo-cover]");
    const add = root.querySelector("[data-photo-add]");
    const entries = [...root.querySelectorAll("[data-photo-existing]")].map(el => ({
      key: el.dataset.photoExisting, url: el.dataset.photoUrl
    }));
    let active = 0, busy = false;
    const error = message => { status.textContent = message || ""; status.hidden = !message; };
    function sync() {
      const transfer = new DataTransfer();
      let newIndex = 0;
      orderInput.value = entries.map(entry => {
        if (!entry.file) return "e:" + entry.key;
        transfer.items.add(entry.file);
        return "n:" + newIndex++;
      }).join(",");
      fileInput.files = transfer.files;
    }
    function draw() {
      sync();
      active = Math.min(Math.max(0, active), Math.max(0, entries.length - 1));
      const current = entries[active];
      counter.textContent = entries.length ? (active + 1) + " de " + entries.length + (active === 0 ? " · Fotografia de capa" : "") : "0 de 5";
      image.hidden = placeholder.hidden = !!current;
      placeholder.hidden = !!current;
      image.hidden = !current;
      if (current) { image.src = current.url; image.alt = "Fotografia " + (active + 1) + " do artigo"; }
      [prev, next].forEach(button => button.disabled = entries.length < 2 || busy);
      [remove, left, right, cover].forEach(button => button.disabled = !current || busy);
      left.disabled = !current || busy || active === 0;
      cover.disabled = !current || busy || active === 0;
      right.disabled = !current || busy || active === entries.length - 1;
      add.disabled = entries.length >= MAX_FILES || busy;
    }
    function move(to) {
      if (to < 0 || to >= entries.length || to === active) return;
      const entry = entries.splice(active, 1)[0];
      entries.splice(to, 0, entry);
      active = to; draw();
    }
    add.addEventListener("click", () => fileInput.click());
    prev.addEventListener("click", () => { active = (active + entries.length - 1) % entries.length; draw(); });
    next.addEventListener("click", () => { active = (active + 1) % entries.length; draw(); });
    left.addEventListener("click", () => move(active - 1));
    right.addEventListener("click", () => move(active + 1));
    cover.addEventListener("click", () => move(0));
    remove.addEventListener("click", () => {
      const [removed] = entries.splice(active, 1);
      clean(removed); error(""); draw();
    });
    fileInput.addEventListener("change", async () => {
      const picked = Array.from(fileInput.files || []);
      if (!picked.length) return;
      error(""); busy = true; draw();
      try {
        for (const file of picked) {
          if (entries.length >= MAX_FILES) { error("Podes adicionar no máximo cinco fotografias."); break; }
          const compressed = await optimize(file);
          if (compressed.size > MAX_BYTES) { error("A imagem " + file.name + " continua acima dos 5 MB. Usa uma fotografia menor."); continue; }
          entries.push({ file: compressed, url: URL.createObjectURL(compressed) });
          active = entries.length - 1;
        }
      } catch (e) { error(e.message || "Erro ao ler a fotografia."); }
      finally { busy = false; draw(); }
    });
    form?.addEventListener("submit", e => {
      if (busy || !entries.length) {
        e.preventDefault();
        error(busy ? "Aguarda a preparação das fotografias." : "Adiciona pelo menos uma fotografia.");
        return;
      }
      sync();
    });
    window.addEventListener("pagehide", () => entries.forEach(clean), { once: true });
    draw();
  });
  document.querySelectorAll("[data-photo-carousel-view]").forEach(root => {
    const urls = [...root.querySelectorAll("[data-view-photo]")].map(el => el.dataset.viewPhoto);
    const main = root.querySelector("[data-view-main]");
    const counter = root.querySelector("[data-view-counter]");
    const previous = root.querySelector("[data-view-prev]");
    const next = root.querySelector("[data-view-next]");
    if (!main || !urls.length) return;
    let index = 0;
    const draw = () => {
      main.src = urls[index]; main.alt = "Fotografia " + (index + 1) + " de " + urls.length;
      if (counter) counter.textContent = (index + 1) + " / " + urls.length;
      if (previous) previous.hidden = urls.length < 2;
      if (next) next.hidden = urls.length < 2;
    };
    previous?.addEventListener("click", () => { index = (index + urls.length - 1) % urls.length; draw(); });
    next?.addEventListener("click", () => { index = (index + 1) % urls.length; draw(); });
    draw();
  });
})();