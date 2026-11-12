// End-to-end browser check of the RentAll rental journey with two real accounts
// (plus an unrelated third account for authorization checks).
//
// Runs against a locally started RentAll + PostgreSQL built from database/*.sql.
// Usage:  BASE_URL=http://localhost:5080 node tests/E2E/rental-journey.e2e.js
// The X-Forwarded-Proto header mirrors Render's TLS-terminating proxy.
const { chromium } = require('playwright');
const path = require('path');
const assert = require('assert');

const BASE = process.env.BASE_URL || 'http://localhost:5080';
const SHOTS = process.env.SHOT_DIR || '';
const stamp = Date.now().toString(36);
const PASSWORD = 'Teste#2026ra';
const photoFixture = path.join(__dirname, '../../wwwroot/catalogo/camera.jpg');
const log = (...a) => console.log('•', ...a);

async function newUser(browser, first, viewport) {
  const ctx = await browser.newContext({
    extraHTTPHeaders: { 'X-Forwarded-Proto': 'https' },
    viewport: viewport || { width: 1280, height: 900 }
  });
  const page = await ctx.newPage();
  page.on('pageerror', e => { throw new Error(`[${first}] JS error: ${e.message}`); });
  const email = `${first.toLowerCase()}.${stamp}@example.test`;
  await page.goto(`${BASE}/Identity/Account/Register`);
  await page.fill('#Input_FirstName', first);
  await page.fill('#Input_LastName', 'Teste');
  await page.fill('#Input_Email', email);
  await page.fill('#Input_Password', PASSWORD);
  await page.fill('#Input_ConfirmPassword', PASSWORD);
  await page.check('#AcceptTerms');
  await Promise.all([page.waitForNavigation(), page.click('form button[type=submit].auth-wide')]);
  const html = await page.content();
  assert(!/Registar|Criar conta/.test(await page.title()) || html.includes('Terminar sessão') || html.includes('Sair'),
    `${first}: registo não concluído`);
  log(`${first} registado (${email})`);
  return { ctx, page, email, name: first };
}

async function flash(page) {
  const ok = await page.locator('.notice-success').allInnerTexts();
  const err = await page.locator('.notice-danger, .validation-summary-errors').allInnerTexts();
  return { ok: ok.join(' ').trim(), err: err.join(' ').trim() };
}

async function shot(page, name) {
  if (SHOTS) await page.screenshot({ path: path.join(SHOTS, `${name}.png`), fullPage: true });
}

async function publicDemo(browser) {
  const page = await browser.newPage({ viewport: { width: 390, height: 844 } });
  try {
    await page.goto(`${BASE}/exemplos`);
    const photos = await page.locator('.listing-image img').evaluateAll(images => images.map(image => image.getAttribute('src')));
    assert.strictEqual(photos.length, 22);
    assert.strictEqual(new Set(photos).size, photos.length, 'Fotos repetidas no catálogo de demonstração');
    await page.locator('a[aria-label="Simular aluguer de Projetor Full HD"]').click();
    await page.locator('[data-quick-days]').fill('4');
    assert.match(await page.locator('[data-quick-total]').first().innerText(), /88,00/);
    await page.getByRole('button', { name: 'Simular pagamento' }).click();
    await page.getByRole('button', { name: 'Simular recolha' }).click();
    await page.getByRole('button', { name: 'Simular devolução' }).click();
    assert(await page.getByRole('heading', { name: 'Simulação concluída' }).isVisible());
    await page.getByRole('link', { name: 'Ver percurso completo' }).click();
    assert(await page.getByRole('heading', { name: 'Alugar Projetor Full HD' }).isVisible());
    const pickup = await page.locator('[data-demo-pickup]').inputValue();
    const dropoff = new Date(Date.parse(pickup + 'T00:00:00Z') + 4 * 86400000).toISOString().slice(0, 10);
    await page.locator('[data-demo-dropoff]').fill(dropoff);
    assert.match(await page.locator('[data-demo-screen="0"] [data-demo-total]').innerText(), /88,00/);
    await page.locator('[data-demo-ack]').check();
    await page.locator('[data-demo-next]').click();
    await page.locator('[data-demo-authorize]').click();
    await page.locator('[data-demo-after-pay]').click();
    await page.locator('[data-demo-delivery-place]').fill('Estação de Coimbra');
    await page.locator('[data-demo-delivery-form] button[type=submit]').click();
    await page.locator('[data-demo-confirm-delivery]').click();
    await page.locator('[data-demo-handover-check]').check();
    await page.locator('[data-demo-handover="owner"]').click();
    await page.getByRole('button', { name: 'Confirmar receção do artigo' }).click();
    assert(await page.getByRole('heading', { name: 'Artigo recebido na simulação' }).isVisible());
    await page.locator('[data-demo-start-return]').click();
    await page.locator('[data-demo-return-check]').check();
    await page.locator('[data-demo-return="renter"]').click();
    await page.locator('[data-demo-return="owner"]').click();
    assert(await page.getByRole('heading', { name: 'Aluguer demonstrativo concluído' }).isVisible());
    const receipt = page.locator('#comprovativo-demo');
    assert.match(await receipt.innerText(), /88,00/);
    assert.match(await receipt.innerText(), /Estação de Coimbra/);
    assert.match(await receipt.locator('[data-demo-reference]').innerText(), /^DEMO-[A-F0-9]{8}$/);
    log('Catálogo público: 22 fotos distintas e simulação individual até à devolução');
  } finally {
    await page.close();
  }
}

// Submits the demo-journey form that carries the given operation.
async function operacao(page, op, fill) {
  const form = page.locator(`form:has(input[name=operacao][value="${op}"]), form:has(button[name=operacao][value="${op}"])`).first();
  assert(await form.count(), `Formulário da operação "${op}" não encontrado`);
  // Secondary actions live in collapsed <details> panels.
  await form.evaluate(el => { const d = el.closest('details'); if (d) d.open = true; });
  if (fill) await fill(form);
  const byValue = form.locator(`button[value="${op}"]`);
  const btn = (await byValue.count()) ? byValue.first() : form.locator('button[type=submit]').first();
  await Promise.all([page.waitForNavigation(), btn.click()]);
  return flash(page);
}

(async () => {
  const browser = await chromium.launch({ executablePath: process.env.CHROMIUM_EXECUTABLE });
  try {
    await publicDemo(browser);
    const anunciante = await newUser(browser, 'Ana');
    const locatario = await newUser(browser, 'Bruno', { width: 390, height: 844 });
    const intruso = await newUser(browser, 'Carla');

    // 1. Publish a listing with a photo.
    const a = anunciante.page;
    await a.goto(`${BASE}/Publicar`);
    await a.fill('#Nome', `Projetor HD ${stamp}`);
    await a.fill('#Descricao', 'Projetor em bom estado, inclui cabo HDMI e comando.');
    await a.selectOption('#Categoria', { index: 1 });
    await a.waitForFunction(() => document.querySelector('#Tipo') && !document.querySelector('#Tipo').disabled && document.querySelector('#Tipo').options.length > 1);
    await a.selectOption('#Tipo', { index: 1 });
    if (await a.locator('#Pais').count()) await a.selectOption('#Pais', 'PT').catch(() => {});
    await a.waitForFunction(() => document.querySelector('#Estado') && document.querySelector('#Estado').options.length > 1);
    await a.selectOption('#Estado', 'Lisboa');
    await a.fill('#Cidade', 'Lisboa');
    await a.fill('#Preco', '12.50');
    await a.fill('#DiasAluguel', '14');
    await a.setInputFiles('input[data-photo-files]', photoFixture);
    await shot(a, '01-publicar');
    await Promise.all([a.waitForNavigation(), a.click('form.ra-publish-form button[type=submit]')]);
    let f = await flash(a);
    const m = a.url().match(/Anuncios\/Details\/(\d+)/i);
    assert(m, `Publicação falhou: ${a.url()} ${f.err}`);
    const anuncioId = m[1];
    log(`Anúncio ${anuncioId} publicado com fotografia`);

    // 2. Renter finds it, messages and requests 3 days.
    const b = locatario.page;
    await b.goto(`${BASE}/?SearchString=${encodeURIComponent(stamp)}`);
    assert.strictEqual(await b.locator('.listing-card').count(), 1, 'Pesquisa não encontrou o anúncio publicado');
    const favorite = b.locator('.listing-card .fav-button');
    await Promise.all([b.waitForNavigation(), favorite.click()]);
    await b.goto(`${BASE}/Favoritos`);
    assert.strictEqual(await b.locator('.listing-card').count(), 1, 'Anúncio não entrou nos favoritos');
    await Promise.all([b.waitForNavigation(), b.locator('.listing-card .fav-button').click()]);
    assert.strictEqual(await b.locator('.listing-card').count(), 0, 'Anúncio não saiu dos favoritos');
    await b.goto(`${BASE}/Anuncios/Details/${anuncioId}`);
    await shot(b, '02-anuncio-mobile');
    await b.goto(`${BASE}/Mensagens/Conversa?anuncioId=${anuncioId}`);
    await b.fill('textarea[name=texto]', 'Olá! Está disponível no próximo fim de semana?');
    await Promise.all([b.waitForNavigation(), b.locator('form:has(textarea[name=texto]) button[type=submit]').click()]);
    assert((await b.content()).includes('próximo fim de semana'), 'Mensagem não apareceu na conversa');
    log('Mensagem enviada e visível na conversa');

    // Contact details must be refused.
    await b.fill('textarea[name=texto]', 'Liga-me para 912345678');
    await b.locator('form:has(textarea[name=texto]) button[type=submit]').click();
    await b.locator('[data-message-warning]').waitFor({ state: 'visible' });
    assert.strictEqual(await b.locator('textarea[name=texto]').inputValue(), 'Liga-me para 912345678');
    await b.fill('textarea[name=texto]', 'Podemos combinar a entrega na estação?');
    await Promise.all([b.waitForNavigation(), b.locator('form:has(textarea[name=texto]) button[type=submit]').click()]);
    assert((await b.locator('.ra-chat-history').innerText()).includes('entrega na estação'), 'Mensagem corrigida não foi enviada');
    await b.goto(`${BASE}/Mensagens/Conversa?anuncioId=${anuncioId}`);
    assert(!(await b.content()).includes('912345678'), 'Mensagem com telefone foi guardada');
    log('Mensagem com telefone recusada (não persistida)');

    await b.goto(`${BASE}/Anuncios/Details/${anuncioId}`);
    await b.fill('#rental-days', '3');
    await Promise.all([b.waitForNavigation(), b.locator('form[action*="/Pedidos/Criar"] button[type=submit]').click()]);
    f = await flash(b);
    assert(f.ok.includes('Pedido enviado'), `Pedido não criado: ${f.err}`);
    const pedidoId = await b.locator('input[name=id]').first().getAttribute('value');
    log(`Pedido ${pedidoId} criado (3 dias)`);

    // Duplicate request must be blocked.
    await b.goto(`${BASE}/Anuncios/Details/${anuncioId}`);
    await b.fill('#rental-days', '2');
    await Promise.all([b.waitForNavigation(), b.locator('form[action*="/Pedidos/Criar"] button[type=submit]').click()]);
    f = await flash(b);
    assert(f.err.includes('negociação em curso'), 'Pedido duplicado não foi bloqueado');
    log('Pedido duplicado bloqueado');

    // 3. Owner counter-offers 4 days at 11€, renter accepts.
    await a.goto(`${BASE}/Pedidos?vista=recebidos`);
    const propor = a.locator(`form[action*="/Pedidos/Propor"]:has(input[name=id][value="${pedidoId}"])`);
    await a.locator('details.ra-counteroffer-details', { has: propor }).locator('summary').click();
    await propor.locator('input[name=dias]').fill('4');
    await propor.locator('input[name=precoDia]').fill('11');
    await Promise.all([a.waitForNavigation(), propor.locator('button[type=submit]').click()]);
    f = await flash(a);
    assert(f.ok.includes('Contraproposta registada'), `Contraproposta falhou: ${f.err}`);
    log('Contraproposta do anunciante registada');

    await b.goto(`${BASE}/Mensagens`);
    assert(await b.getByText('Pedido por responder', { exact: true }).isVisible(),
      'A caixa de entrada não sinalizou a contraproposta');
    await b.goto(`${BASE}/Pedidos`);
    const decidir = b.locator(`form[action*="/Pedidos/DecidirProposta"]:has(input[name=id][value="${pedidoId}"]):has(input[name=aceitar][value=true]), form[action*="/Pedidos/DecidirProposta"]:has(input[name=id][value="${pedidoId}"]):has(button[name=aceitar][value=true])`).first();
    const versao = decidir.locator('input[name=versaoProposta]');
    const versaoAtual = await versao.inputValue();
    await versao.evaluate(input => { input.value = '1'; });
    await Promise.all([b.waitForNavigation(), decidir.locator('button').first().click()]);
    f = await flash(b);
    assert(f.err.includes('contraproposta mudou'), 'Uma resposta obsoleta foi aceite');
    assert.strictEqual(await versao.inputValue(), versaoAtual, 'O pedido mudou após a tentativa obsoleta');
    await Promise.all([b.waitForNavigation(), decidir.locator('button').first().click()]);
    f = await flash(b);
    assert(f.ok.includes('Contraproposta aceite'), `Aceitação falhou: ${f.err}`);
    log('Locatário aceitou a contraproposta (44,00 €)');

    // 4. Intruder cannot read or mutate the journey.
    let r = await intruso.page.request.get(`${BASE}/PercursoDemo/${pedidoId}`);
    assert.strictEqual(r.status(), 404, 'Intruso conseguiu abrir o percurso');
    r = await intruso.page.request.get(`${BASE}/Pagamentos/Demonstracao?pedidoId=${pedidoId}`);
    assert.strictEqual(r.status(), 404, 'Intruso conseguiu abrir o checkout do pedido');
    r = await intruso.page.request.get(`${BASE}/Pedidos?pedido=${pedidoId}`);
    assert.strictEqual(r.status(), 404, 'Intruso conseguiu abrir o pedido');
    log('Conta não participante recebe 404 no percurso e no checkout');

    // 5. Persistent journey: checkout, delivery, rental, return, rating.
    await b.goto(`${BASE}/Mensagens/Conversa?anuncioId=${anuncioId}`);
    await b.getByRole('link', { name: 'Ir para pagamento simulado', exact: true }).click();
    await Promise.all([b.waitForNavigation(), b.locator('form[action*="/PercursoDemo/Iniciar"] button[type=submit]').click()]);
    await shot(b, '03-checkout-mobile');
    f = await operacao(b, 'recusar', form => form.locator('[name=metodo]').first().check().catch(() => form.locator('select[name=metodo]').selectOption('cartao')));
    assert(f.ok.includes('Recusa simulada'), `Recusa falhou: ${f.err}`);
    f = await operacao(b, 'aprovar', form => form.locator('[name=metodo]').first().check().catch(() => form.locator('select[name=metodo]').selectOption('mbway')));
    assert(f.ok.includes('Aprovação simulada'), `Aprovação falhou: ${f.err}`);
    log('Checkout simulado: recusa e depois aprovação');
    await shot(b, '04-comprovativo-mobile');

    // Renter must not be able to propose delivery.
    assert.strictEqual(await b.locator('form:has([value="propor"])').count(), 0, 'Locatário vê formulário de proposta');

    await a.goto(`${BASE}/PercursoDemo/${pedidoId}`);
    const future = new Date(Date.now() + 3 * 864e5);
    const local = `${future.getFullYear()}-${String(future.getMonth() + 1).padStart(2, '0')}-${String(future.getDate()).padStart(2, '0')}T18:30`;
    f = await operacao(a, 'propor', async form => {
      await form.locator('[name=tipo]').selectOption('em_maos').catch(() => form.locator('[name=tipo][value=em_maos]').check());
      await form.locator('[name=local]').fill('Estação do Oriente, entrada principal');
      await form.locator('[name=dataHora]').fill(local);
    });
    assert(f.ok.includes('Proposta de entrega'), `Proposta de entrega falhou: ${f.err}`);
    log('Anunciante propôs entrega em mãos');

    await b.goto(`${BASE}/PercursoDemo/${pedidoId}`);
    if (await b.locator('form:has([value="pedir_alteracao"])').count()) {
      f = await operacao(b, 'pedir_alteracao', form => form.locator('[name=nota]').fill('Pode ser às 19h?'));
      assert(f.ok.includes('Pedido de alteração'), `Pedido de alteração falhou: ${f.err}`);
      await a.goto(`${BASE}/PercursoDemo/${pedidoId}`);
      assert((await a.content()).includes('Pode ser às 19h?'), 'Anunciante não vê o pedido de alteração');
      f = await operacao(a, 'propor', async form => {
        await form.locator('[name=tipo]').selectOption('em_maos').catch(() => {});
        await form.locator('[name=local]').fill('Estação do Oriente, entrada principal');
        await form.locator('[name=dataHora]').fill(local.replace('18:30', '19:00'));
      });
      assert(f.ok.includes('Proposta de entrega'), `Nova proposta falhou: ${f.err}`);
      log('Locatário pediu alteração; anunciante enviou nova proposta');
      await b.goto(`${BASE}/PercursoDemo/${pedidoId}`);
    }
    f = await operacao(b, 'aceitar');
    assert(f.ok.includes('Entrega acordada'), `Aceitar entrega falhou: ${f.err}`);

    await a.goto(`${BASE}/PercursoDemo/${pedidoId}`);
    if (await a.locator('form:has([value="checklist_entrega"])').count()) {
      f = await operacao(a, 'checklist_entrega', async form => {
        await form.locator('input[name=estadoArtigo][value=marcas_uso]').check();
        await form.locator('[name=acessorios]').fill('Cabo HDMI, comando, bolsa');
        await form.locator('[name=nota]').fill('Pequeno risco na tampa lateral.');
        await form.locator('input[name=foto]').setInputFiles(photoFixture);
      });
      assert(f.ok.includes('Checklist'), `Checklist falhou: ${f.err}`);
      const html = await a.content();
      assert(html.includes('Cabo HDMI, comando, bolsa') && html.includes('Marcas de uso'), 'Checklist não aparece no histórico');
      assert(await a.locator('.ra-live-photo img').count() === 1, 'Fotografia da checklist não aparece');
      log('Checklist de entrega (estado, acessórios, observações, fotografia) registada');
    }
    f = await operacao(a, 'entregar');
    assert(f.ok.includes('Confirmação guardada'), `Entrega (anunciante) falhou: ${f.err}`);
    await b.goto(`${BASE}/PercursoDemo/${pedidoId}`);
    f = await operacao(b, 'receber');
    assert(f.ok.includes('Confirmação guardada'), `Receção (locatário) falhou: ${f.err}`);
    assert((await b.content()).match(/Em curso|EmCurso|Aluguer em curso/i), 'Estado não passou a em curso');
    log('Entrega confirmada pelas duas partes → aluguer em curso');

    f = await operacao(b, 'incidente', form => form.locator('[name=nota]').fill('O comando deixou de funcionar.').catch(() => {}));
    assert(f.ok.includes('Ocorrência'), `Ocorrência falhou: ${f.err}`);
    f = await operacao(b, 'devolver').catch(e => ({ ok: '', err: String(e) }));
    assert(!f.ok.includes('Devolução registada'), 'Devolução permitida com ocorrência aberta');
    await a.goto(`${BASE}/PercursoDemo/${pedidoId}`);
    f = await operacao(a, 'resolver', form => form.locator('[name=nota]').fill('Pilhas trocadas.').catch(() => {}));
    assert(f.ok.includes('resolvida'), `Resolver falhou: ${f.err}`);
    log(`Histórico de eventos: ${await a.locator('.ra-live-timeline li').count()} entradas`);
    log('Ocorrência aberta bloqueou devolução e foi resolvida');

    await b.goto(`${BASE}/PercursoDemo/${pedidoId}`);
    f = await operacao(b, 'devolver', form => form.locator('[name=nota]').fill('Devolvido com bolsa.').catch(() => {}));
    assert(f.ok.includes('Devolução registada'), `Devolução falhou: ${f.err}`);
    await a.goto(`${BASE}/PercursoDemo/${pedidoId}`);
    if (await a.locator('form:has([value="divergencia"])').count()) {
      f = await operacao(a, 'divergencia', form => form.locator('[name=nota]').fill('Falta a bolsa de transporte.'));
      assert(f.ok.includes('Divergência'), `Divergência falhou: ${f.err}`);
      await b.goto(`${BASE}/PercursoDemo/${pedidoId}`);
      assert((await b.content()).includes('Falta a bolsa de transporte.'), 'Locatário não vê a divergência');
      await a.goto(`${BASE}/PercursoDemo/${pedidoId}`);
      f = await operacao(a, 'resolver', form => form.locator('[name=nota]').fill('Bolsa encontrada.').catch(() => {}));
      assert(f.ok.includes('resolvida'), `Resolver divergência falhou: ${f.err}`);
      log('Divergência na devolução registada e resolvida');
    }
    f = await operacao(a, 'concluir');
    assert(f.ok.includes('concluído'), `Conclusão falhou: ${f.err}`);
    await b.goto(`${BASE}/PercursoDemo/${pedidoId}`);
    f = await operacao(b, 'avaliar', form => form.locator('select[name=avaliacao]').selectOption('5'));
    assert(f.ok.includes('Avaliação'), `Avaliação falhou: ${f.err} | ${f.ok} | ${b.url()} | ${await b.locator('.ra-live-status').innerText()}`);
    await b.reload();
    assert((await b.content()).match(/Conclu[ií]do/), 'Estado concluído não persistiu após recarregar');
    await b.goto(`${BASE}/Mensagens/Conversa?anuncioId=${anuncioId}`);
    assert(await b.getByText('Aluguer demonstrativo concluído', { exact: true }).isVisible(),
      'Conversa não mostra o aluguer concluído');
    await b.getByRole('link', { name: 'Ver resumo do aluguer', exact: true }).click();
    await shot(b, '05-concluido-mobile');
    await a.goto(`${BASE}/PercursoDemo/${pedidoId}`);
    await shot(a, '06-concluido-desktop');
    log('Devolução confirmada, percurso concluído e avaliado; estado persiste após recarregar');

    // 6. Forged POSTs from the intruder are refused.
    const token = await intruso.page.goto(`${BASE}/Pedidos`).then(() =>
      intruso.page.locator('input[name=__RequestVerificationToken]').first().getAttribute('value'));
    if (token) {
      const resp = await intruso.page.request.post(`${BASE}/PercursoDemo/Atualizar`, {
        form: { pedidoId, operacao: 'concluir', __RequestVerificationToken: token },
        headers: { 'X-Forwarded-Proto': 'https' }, maxRedirects: 0
      });
      assert.strictEqual(resp.status(), 404, `POST forjado devolveu ${resp.status()}`);
      log('POST forjado por conta não participante recusado (404)');
    }
    console.log('\nE2E OK — percurso completo verificado com duas contas.');
  } finally {
    await browser.close();
  }
})().catch(e => { console.error('\nE2E FALHOU:', e.message); process.exit(1); });
