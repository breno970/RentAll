# RentAll — Stripe Connect e Payments (sandbox Portugal)

**Implementado no código, sem credenciais conectadas.** Não ativa pagamentos reais. Os pagamentos só estarão disponíveis depois de configuração manual segura e verificação funcional.

## Backend incluído
- SDK Stripe.net e cliente próprio StripeClient.
- Accounts v2 (recipient, Dashboard Express, plataforma responsável pelas taxas e saldos negativos), onboarding alojado na Stripe e verificação ativa de `configuration.recipient.capabilities.stripe_balance.stripe_transfers.status`.
- Checkout Stripe para pedidos aceites, utilizador autenticado, montantes lidos da base de dados, idempotência estável por pedido, fee indicativa de 10%, moeda EUR e transferência para uma conta conectada verificada.
- Webhook `POST /api/stripe/webhook` que valida assinatura da Stripe, verifica ID, moeda e valor, deduplica eventos e atualiza estado numa transação. Processa conclusão paga, pagamento assíncrono bem-sucedido, falha e expiração.
- Tabelas privadas `ContasStripeLocador`, `PagamentosAluguer`, `EventosStripeProcessados`. Sem cartões, IBAN, documentos ou chaves no PostgreSQL.
- Vista `GET /Pagamentos/Conta` e ações autenticadas para onboarding e checkout. Estado no painel de pedidos.

## Segurança e configuração
A configuração fica **desativada** enquanto não houver sandbox validada. Usar variáveis privadas na configuração Render (não publicar os valores):
```
StripePayments__Enabled=false
StripePayments__SecretKey=
StripePayments__WebhookSecret=
StripePayments__PlatformCountry=PT
StripePayments__PublicBaseUrl=https://rentall-m573.onrender.com
StripePayments__TaxEnabled=false
```
Só testar `Enabled=true` numa **sandbox isolada** com chave Stripe de teste (`rk_test_` restrita quando compatível, ou `sk_test_`) e endpoint de eventos separado. NÃO usar chave live. Testar com pelo menos duas contas de teste, eventos de webhook e reembolsos antes de permitir fluxos abertos.

## Impostos
`StripePayments__TaxEnabled=true` **bloqueia a ativação** nesta implementação, deliberadamente: ainda falta identificar entidade tributária responsável e registos ativos, selecionar tax code correto, configurar `automatic_tax.liability` e testar o motivo `taxability_reason`. Não é conforme declarar IVA calculado quando não há registo.

## Não está concluído
- Criar/assumir uma sandbox na tua **conta** Stripe (requer sessão Stripe autorizada).
- Credenciais de sandbox e secret de webhook; ligação do webhook no Stripe Dashboard.
- Reembolsos, chargebacks, monitorização de capacidade v2 e alertas de conta, notificações e reconciliação quando falhar a persistência.
- Testes end-to-end reais com a API Stripe e contas conectadas; aprovação dos termos fiscais e comerciais em Portugal.
- Pagamentos e transferências no Brasil.
- Pagamentos reais, retenção de caução e checkout público permanente.
- Registo de atividade na AT; a app não pode abrir atividade fiscal em teu nome sem identificação e confirmação legal.

**Produção:** não ativar cobranças reais até cumprir todos os pré-requisitos e obter confirmação explícita do dono da plataforma.

Referências: https://docs.stripe.com/connect/marketplace/tasks/create ; https://docs.stripe.com/connect/marketplace/tasks/onboard ; https://docs.stripe.com/connect/destination-charges ; https://docs.stripe.com/checkout/fulfillment ; https://docs.stripe.com/tax/connect
