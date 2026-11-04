# RentAll — receita e retenção de transações (proposta, NÃO ativada)

## Modelo económico recomendado

Lançamento com anúncios gratuitos e conversa dentro da plataforma. O objetivo é conquistar oferta e liquidez antes de cobrar por uso.

1. **Reserva protegida** (futura): aplicar comissão ilustrativa de **10%** ao valor do aluguer efetivamente pago pelo cliente, com taxa visível antes de confirmar. Exemplo: aluguer de €50 → taxa de plataforma €5 **brutos**, antes das taxas de processamento, impostos, reembolsos, fraudes e suporte. A liquidação só é receita líquida depois desses custos.
2. **Serviços opcionais**: destaque do anúncio por prazo delimitado, planos Pro de gestão de inventário para locadores frequentes e proteção opcional se houver parceiro segurador autorizado. Evitar paywalls iniciais para anúncios comuns.
3. **Reserva com proteção**: calendário de disponibilidade, check-in/checkout com prova fotográfica, avaliações verificadas apenas depois de transações, política de cancelamento transparente, suporte e resolução de conflitos. Estes benefícios dão razões reais para não negociar fora da plataforma.
4. **Mensagens internas**: já existe estrutura básica; não revelar contacto público. A filtragem por expressão regular é apenas uma barreira limitada e pode produzir falsos positivos e ser contornada; não substitui moderação humana, antifraude ou uma política clara.

## Pagamentos: nunca improvisar

- Considerar Stripe Connect/alternativa licenciada, contas conectadas, KYC e repasses automáticos. Avaliar regras país a país antes de ativar Portugal e Brasil; fluxos transfronteiriços Brasil têm restrições e não devem ser presumidos possíveis.
- Apenas registar **reservas pagas** depois de webhook autenticado, com idempotência e razão contabilística; nunca confiar apenas no redirecionamento do checkout.
- Distinguir valor do aluguer, caução (se legalmente adequada), comissão, taxa de processamento, impostos, reembolsos e valor líquido do proprietário.
- Validar obrigações fiscais, faturação, impostos indiretos, regras de intermediação, consumo e regras de proteção de pagamentos com assessoria jurídica/contabilística local.
- A conta da plataforma não deve guardar números de cartão, IBAN ou documentos de identidade quando o processador puder fazer esse processamento.

## Anti-desintermediação equilibrada

Não prometer impedir 100% dos negócios fora. Melhor estratégia: proteção de reserva, suporte, confiança, disponibilidade sincronizada, avaliações após aluguer confirmado, promoções nas primeiras reservas e benefícios a quem mantém pagamentos na plataforma. Evitar alegações de "garantia" não contratada.

## Métricas

GMV pago, reservas concluídas, take rate bruto e líquido, percentagem de repetição, conversões chat→reserva, custo de suporte, disputas/chargebacks, margem por país, CAC e retenção.

## Estado real

**Nenhum checkout, cobrança, comissão automática, caução ou seguro está ativo.** As conversas básicas são apenas preparação da experiência. Lançar pagamentos apenas após aprovação do modelo, integração do processador e testes regulatórios.

Referências: https://stripe.com/connect/pricing ; https://support.stripe.com/questions/transaction-declined-stripe-brazil-accounts?locale=en-GB .
