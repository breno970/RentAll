# RentAll — pedidos, segurança e modelo de receitas

## Entregue neste incremento
- Os novos anúncios estão **Ativos** por predefinição, mas cada anunciante pode optar por guardá-los ocultos; o servidor conserva o valor "S" salvo escolha explícita "N".
- Preço ilustrativo de aluguer e simulação de receita bruta e valor após uma possível comissão de 10%; os valores são apresentados na moeda do país.
- Pedidos de aluguer autenticados: o interessado escolhe o número de dias, o anúncio identifica o proprietário, o pedido fica pendente e o dono pode aceitar ou recusar. O interessado pode cancelar enquanto está pendente. Ambos acompanham o histórico.
- Preço e comissão indicativos ficam registados como fotografia do valor no momento do pedido. **Nenhum pagamento é processado**, não há caução, reservas pagas, escrow ou garantias de disponibilidade.
- Segurança: um utilizador não pode alugar o seu próprio anúncio; não pode escolher dias inválidos ou duplicar pedidos pendentes; o proprietário é validado no servidor antes de responder; throttling por conta.
- Anúncios podem ser denunciados por titulares autenticados. As denúncias são privadas e só o papel ASP.NET Identity `Admin` pode abrir a fila de moderação. Não existe remoção automática a partir de uma denúncia.
- Listagens divulgam cuidados práticos para o levantamento/entrega, fotografias, verificação do artigo e comunicações sem contacto pessoal.

## Modelo de ganhos proposto, ainda NÃO ativado
- **10% sobre o valor de cada aluguer pago dentro da RentAll**, idealmente com pagamento marketplace integrado, identidade do locador verificada, split de fundos e taxas claramente discriminadas.
- A comissão só deverá ser finalizada após pagamento comprovado através de webhook validado e idempotente, reembolsos, custos de processamento e impostos considerados.
- Marketplace de dois países: verificar separadamente limites do processador para Portugal/Brasil, contas conectadas, pagamentos transfronteiriços, impostos e obrigações legais.
- Depois, adicionar planos opcionais de destaque por duração definida e planos Pro para anunciantes recorrentes. A publicação base deve continuar gratuita.
- Para incentivar transações internas, oferecer vantagens genuínas: chat persistente, pedidos rastreáveis (entregue), pagamentos integrados, check-in/out fotográfico, avaliações verificadas, apoio e resolução de disputas (restante por implementar).

## O que falta antes de cobrar
1. Conta de prestador de pagamentos verificada (por exemplo Stripe Connect, com elegibilidade e custos país a país).
2. Contratos e política de cancelamento/devolução/caução, faturação, proteção de dados, KYC/AML e procedimentos contra fraude.
3. Fluxos de checkout seguro, webhook, repasses, reconciliação, reembolsos, retenção limitada e verificação de identidade.
4. Testes E2E com contas de compradores e anunciantes, além de testes de concorrência, CSRF e limites de requisição.

**O sucesso do build não prova que exista cobrança real. Nunca marcar um pedido como pago a partir de um clique do cliente ou de uma mensagem do interessado.**

Referências: https://stripe.com/connect ; https://www.olx.pt/howitworks/ ; https://olxpay.olx.com.br/s/article/anunciar-entrega-e-pagamento
