# RentAll — Stripe Connect: estado da implementação

## Estado atual
Esta branch contém apenas a **base técnica desativada** para pagamento Stripe em modo de testes:
- Modelo de registo de conta conectada do vendedor e tentativas de pagamento.
- Opções de configuração com `Enabled=false` por omissão e rejeição de chaves live.
- Cliente oficial `Stripe.net` para criação de contas Express, onboarding e Checkout com `application_fee_amount` de 10%, transferido para uma conta conectada.
- Não existem endpoints públicos de checkout, nem receção de eventos ligada, nem tabelas criadas no banco. **Não fazer merge/deploy como integração final.**

## Pré-condições
1. Criar/conectar uma conta Stripe Connect elegível para marketplace em Portugal. Não colocar chaves nem documentos no código ou na conversa.
2. Configurar credenciais apenas como variáveis privadas no Render: `StripePayments__Enabled=true`, `StripePayments__SecretKey=sk_test_...`, `StripePayments__WebhookSecret=whsec_...`, `StripePayments__PlatformCountry=PT`.
3. Ativar o fluxo de onboarding com verificação de titularidade e contas de repasse através das páginas alojadas pela Stripe.
4. Implementar controlador protegido para pedido aceite e comprador autenticado, reconciliar preços com o servidor e criar Checkout Session.
5. Implementar webhook que valide a assinatura Stripe, valide sessão/valor/moeda/proprietário, registe o identificador único do evento e aceite retries sem duplicar o estado.
6. Registar `checkout.session.completed`, `checkout.session.async_payment_succeeded`, expiração, reembolsos e disputas; reconciliar a conta e só declarar pago depois de confirmação.
7. Validar testes automatizados e manuais em modo de testes, incluindo pagamentos repetidos, expiração, CSRF, chargebacks, falha do provider e proteção de dados.
8. Rever termos, fiscalidade e elegibilidade do país Brasil; a configuração inicial é exclusivamente Portugal e EUR.

## Modelo económico
Para um aluguer de €50, comissão hipotética de 10% = €5, montante bruto remanescente do anunciante €45, **antes das comissões do prestador, impostos, disputas e reembolsos**. É necessário revelar as taxas ao utilizador antes de pagar.

## Bloqueador
Os endpoints de cobrança e confirmação de pagamentos ainda não puderam ser integrados com segurança neste ambiente. A tentativa de adicionar os controladores foi bloqueada; nunca afirmar que estão disponíveis ou ligados ao Render.

Referências:
- https://docs.stripe.com/connect/destination-charges
- https://docs.stripe.com/connect/express-accounts
- https://docs.stripe.com/webhooks
- https://stripe.com/connect/pricing
