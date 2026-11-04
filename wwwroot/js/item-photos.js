(() => {
  document.querySelectorAll('[data-photo-picker]').forEach(root => {
    const input = root.querySelector('[data-photo-input]');
    const stage = root.querySelector('[data-photo-stage]');
    const count = root.querySelector('[data-photo-count]');
    const error = root.querySelector('[data-photo-error]');
    const add = root.querySelector('[data-photo-add]');
    const previous = root.querySelector('[data-photo-prev]');
    const next = root.querySelector('[data-photo-next]');
    const files = [];
    const existing = [...root.querySelectorAll('[data-existing-photo]')].map(el => ({ name: el.dataset.existingPhoto, url: el.src }));
    let active = 0;
    function all() { return [...existing.map(e => ({ ...e, existing: true })), ...files.map(f => ({ file: f, url: URL.createObjectURL(f) }))]; }
    function sync() {
      const transfer = new DataTransfer();
      files.forEach(f => transfer.items.add(f));
      input.files = transfer.files;
      root.querySelectorAll('input[name="existingImages"]').forEach(el => el.remove());
      existing.forEach(e => {
        const hidden = document.createElement('input');
        hidden.type = 'hidden'; hidden.name = 'existingImages'; hidden.value = e.name;
        root.appendChild(hidden);
      });
    }
    function draw() {
      sync();
      const items = all();
      active = Math.max(0, Math.min(active, items.length - 1));
      stage.replaceChildren();
      count.textContent = items.length + ' / 5 fotografias';
      previous.disabled = next.disabled = items.length < 2;
      if (!items.length) {
        const empty = document.createElement('p'); empty.textContent = 'Adiciona fotografias para ver a pré-visualização.';
        stage.appendChild(empty);
        return;
      }
      const item = items[active];
      const picture = document.createElement('img');
      picture.src = item.url; picture.alt = 'Fotografia ' + (active + 1); picture.className = 'photo-preview-image';
      stage.appendChild(picture);
      const actions = document.createElement('div'); actions.className = 'photo-preview-actions';
      function button(label, fn, disabled = false) {
        const b = document.createElement('button'); b.type = 'button'; b.textContent = label; b.disabled = disabled;
        b.addEventListener('click', fn); actions.appendChild(b);
      }
      button('Definir como capa', () => { move(active, 0); }, active === 0);
      button('Mover para esquerda', () => move(active, active - 1), active === 0);
      button('Mover para direita', () => move(active, active + 1), active === items.length - 1);
      button('Remover', () => {
        if (active < existing.length) existing.splice(active, 1);
        else files.splice(active - existing.length, 1);
        draw();
      });
      stage.appendChild(actions);
      const position = document.createElement('p'); position.className = 'photo-position';
      position.textContent = 'Fotografia ' + (active + 1) + ' de ' + items.length + (active === 0 ? ' · Capa' : '');
      stage.appendChild(position);
      // Release temporary preview object URLs after browser has loaded images.
      items.forEach((entry, index) => {
        if (!entry.existing && index !== active) URL.revokeObjectURL(entry.url);
      });
    }
    function move(from, to) {
      const items = [...existing.map(e => ({ kind: 'old', value: e })), ...files.map(f => ({ kind: 'file', value: f }))];
      const [element] = items.splice(from, 1);
      items.splice(to, 0, element);
      // Keep the ordered five-slot database representation: existing photos must precede new
      // files until submission; mixed reordering is normalized on the server after upload.
      existing.splice(0, existing.length, ...items.filter(i => i.kind === 'old').map(i => i.value));
      files.splice(0, files.length, ...items.filter(i => i.kind === 'file').map(i => i.value));
      active = Math.max(0, Math.min(to, existing.length + files.length - 1));
      draw();
    }
    add.addEventListener('click', () => input.click());
    previous.addEventListener('click', () => { active = (active - 1 + existing.length + files.length) % (existing.length + files.length); draw(); });
    next.addEventListener('click', () => { active = (active + 1) % (existing.length + files.length); draw(); });
    input.addEventListener('change', () => {
      const picked = [...input.files];
      error.textContent = '';
      for (const file of picked) {
        if (existing.length + files.length >= 5) { error.textContent = 'Limite de 5 fotografias atingido.'; break; }
        if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type) || file.size > 8 * 1024 * 1024 || !file.size) {
          error.textContent = 'Utiliza JPEG, PNG ou WebP até 8 MB por fotografia.'; continue;
        }
        files.push(file);
      }
      active = existing.length + files.length - 1;
      draw();
    });
    root.closest('form')?.addEventListener('submit', event => {
      if (!existing.length && !files.length) { event.preventDefault(); error.textContent = 'Seleciona pelo menos uma fotografia.'; }
      else sync();
    });
    draw();
  });
})();
