# Plano de integração Stripe para a RentAll — Payments + Tax
**Produto:** https://rentall-m573.onrender.com/ — marketplace de aluguer de artigos em desuso. ASP.NET Core 10, EF Core, PostgreSQL/Supabase, Render. **Estado:** especificação e scaffold não publicado; sem pagamentos reais.

## 1. Modelo recomendado
- Marketplace: comprador paga à RentAll, a RentAll retém comissão e encaminha o restante ao anunciante.
- Configurar Stripe Connect **Accounts v2**, Dashboard **Express**, responsabilidades por taxas de processamento: **plataforma** (`fees_collector=application`), responsabilidade por saldos negativos: **plataforma** (`losses_collector=application`), recebimento: `configuration.recipient` com capacidade `stripe_transfers`. Não criar contas v1 com `type=express` nem pedir `configuration.merchant` sem necessidade.
- Para aluguer simples de um só anunciante, **destination charges** com Stripe Checkout hospedado. Comissão inicial proposta **10% do total do aluguer** via `payment_intent_data.application_fee_amount`. Evitar duplicar taxas com Platform Pricing Tool. Conferir margens: plataforma assume taxas de processamento e exposição a disputas/saldos negativos. A plataforma é merchant of record nesse padrão, sujeito a revisão legal.
- **Se for necessário reter o pagamento até ao retorno do item**, em vez de transferência imediata, migrar para **separate charges and transfers**. Nesse padrão NÃO usar `application_fee_amount`; calcular repasse líquido. Determinar timing legal e operacional antes de escolher este caminho. A RentAll ainda não possui confirmação de entrega/devolução.
- Começar com Portugal / EUR e contas conectadas elegíveis em Portugal; o Brasil / BRL e transferências entre países requerem análise específica de disponibilidade, câmbio e compliance. Não inferir elegibilidade só a partir do endereço do utilizador.

## 2. Fluxos Payments
1. **Onboarding anunciante:** criar conta v2 com configuração `recipient`, acesso Express, e identificação/KYC via Stripe-hosted onboarding. Guardar apenas identificador da conta, país, estados de verificação e timestamps. Mostrar aviso de requisitos através de componente `notification_banner` e proporcionar acesso às configurações da conta.
2. **Pedido:** o comprador seleciona prazo; o anunciante aceita; servidor valida proprietário, estado, país, total, limite de dias, preço imutável e conta conectada verificada. Não aceitar total ou comissão do browser.
3. **Checkout:** criar Stripe Checkout Session de pagamento único, linhas com moeda correta, preço e descrição do item, transferência para o anunciante e comissão explícita. Usar métodos de pagamento dinâmicos, não especificar `payment_method_types`. Usar chave de idempotência estável por tentativa; manter correlação local pedido↔sessão.
4. **Webhooks:** endpoint HTTPS dedicado, corpo bruto, assinatura verificada com secret de webhook e tolerância temporal, deduplicação persistente de eventos, transação de DB, validação do valor/moeda/sessão/pedido e atualizações monotónicas de estado; devolver 2xx só depois de persistir. Processar `checkout.session.completed` quando pago, `checkout.session.async_payment_succeeded`, falhas/expiração, reembolsos, disputas e eventos relevantes de conta/conectividade.
5. **Pós-pagamento:** o redirect de sucesso mostra `processamento` até o webhook confirmar; emitir recibos, notificar participantes sem informações sensíveis, acompanhar entregas e devoluções e tratar reembolsos por operador autorizado. Never release article solely based on URL return.
6. **Conciliação:** trilho de auditoria com Stripe Session, PaymentIntent, Charge, Transfer, Application Fee, Refund e Dispute; jobs de reconciliação para webhooks atrasados e interrupções.

## 3. Modelo de dados
- `ContaStripeLocador`: user ID, connected account ID, país, estado de onboarding, capacidade v2, timestamps.
- `PagamentoAluguer`: pedido/participantes, moeda, total, comissão, saldo do anunciante, session/intent, estado, timestamps, versões.
- `EventoStripeProcessado`: event ID único, tipo, timestamp, resultado.
- `ReembolsoAluguer` e `ContestacaoAluguer`: IDs de fornecedor, valores e estados, idempotência.
- Guardar todos os preços em centavos/unidades menores (inteiros), nunca `float` para cálculos de pagamento; migração da atual coluna `vlitem` para um valor decimal validado em servidor. Armazenar snapshots para alterações de anúncios não afetarem pedidos já aceites.
- Restringir tabelas ao papel técnico `rentall_app`; sem acesso direto `anon`/`authenticated`; CSRF e verificação de proprietário nos endpoints ASP.NET.

## 4. Stripe Tax — NÃO ativar ainda
- Primeiro determinar, com contabilista/assessoria em Portugal/Brasil, **quem é legalmente responsável pela cobrança e entrega do imposto**: RentAll, anunciante, ou ambos em operações diferentes. As taxas/impostos da comissão da RentAll podem ter enquadramento diferente do aluguer do bem físico.
- Consultar Tax Settings e endereço da sede; ativar registos tributários apenas depois de confirmar obrigações legais; registo no painel Stripe não substitui inscrição na autoridade tributária.
- Escolher tax code na lista oficial para **aluguer de bens móveis físicos** aplicável ao bem e a entidade responsável, sem inventar `txcd_...`. Distinguir categorias e possíveis exceções.
- Só ativar `automatic_tax.enabled=true` quando existirem registos marcados **Collecting**, conta e responsabilidade corretas, produto com tax code e tax_behavior adequados e morada do comprador recolhida no Checkout quando necessária. Configurar `automatic_tax.liability` de acordo com o responsável legal (`self` ou `account`).
- Validar cálculo em sandbox no país do comprador e inspecionar `taxability_reason`; `not_collecting` **não** significa conformidade. Monitorizar limiares/nexus e relatórios; prever reembolsos e reversões.
- Evitar expor morada exata do **anunciante**: os dados fiscais necessários ao checkout são recolhidos pela Stripe, na etapa apropriada, com transparência na política de privacidade.

## 5. Segurança e operações
- Produção desligada por padrão: `StripePayments__Enabled=false`; iniciar em sandbox isolada. Dar preferência a chave restrita `rk_test_...` de permissões mínimas, guardada no Render, não no repositório, browser, logs ou chat.
- Outras variáveis privadas: `StripePayments__WebhookSecret`, `StripePayments__PlatformCountry`, `StripePayments__PublicBaseUrl`, `StripePayments__TaxEnabled=false`; isolar credenciais de teste e produção.
- Criar endpoints `POST /Pagamentos/Conectar`, `POST /Pagamentos/Checkout`, `POST /api/stripe/webhook`, e vistas de gestão; só ligar quando estiverem implementados e testados. Não publicar um botão que redirecione sem webhook funcional.
- Política de cancelamento, cauções, atrasos e danos; prevenção de fraude e verificações de utilizadores; GDPR/LGPD, termos e transparência de comissões antes do pagamento.
- Testes E2E: recusa de localização, KYC incompleto, conta bloqueada, compra duplicada, sessão expirada, webhooks repetidos/fora de ordem, pagamento assíncrono, reembolso parcial/total, disputas, imposto zero por falta de registo, erros de fornecedor e recuperação de falhas.
- A ativação de pagamentos reais exige confirmação do responsável pela RentAll, KYC e taxas/termos aprovados, e testes em sandbox. Não aceitar `sk_live_` em estágio inicial.

## 6. Auditoria do código atual — PR #8, não integrado
- **Encontrado:** `StripeTestGateway.cs` criava conta com API v1 `type=express`; descontinuado no novo plano e explicitamente bloqueado até implementar Accounts v2.
- **Corrigido:** removido `PaymentMethodTypes=["card"]` para métodos dinâmicos; pacote Stripe.net alinhado com a versão documentada no guia disponível; preferência por chaves de teste restritas.
- **Existe:** estimativa de 10% no modelo de pedido; tipos de registo de contas/pagamentos sem endpoints produtivos.
- **Falta:** Accounts v2 onboarding, endpoint de checkout autenticado, verificação de capacidades v2, webhooks assinados com transações, migrations, integrações de refund/disputa, configuração Tax e teste de contas/repasses.
- **Nenhum pagamento está ligado à produção.** PR deve permanecer draft até segurança funcional completa.

**Referências:** https://docs.stripe.com/connect/marketplace/tasks/create | https://docs.stripe.com/connect/accounts-v2/connected-account-configuration | https://docs.stripe.com/connect/destination-charges | https://docs.stripe.com/tax/connect | https://docs.stripe.com/tax/checkout | https://docs.stripe.com/webhooks | https://docs.stripe.com/keys
