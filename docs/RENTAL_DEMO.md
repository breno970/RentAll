# RentAll — demonstração completa de um aluguer

Esta experiência foi construída para apresentação do conceito RentAll. **Não é uma transação real**. A aplicação mantém, em separado, a integração Stripe **exclusivamente em modo de teste** que já existia. O percurso visual não invoca a Stripe nem o backend de pagamentos.

## Abrir a experiência

- Página pública: \`/Pagamentos/Demonstracao\` (sem necessidade de login).
- Associada a um pedido: \`/Pagamentos/Demonstracao?pedidoId=123\`, apenas para os participantes autenticados do pedido. Os dados do pedido são usados no resumo, sem alterar o seu estado.
- O histórico é local ao navegador e reinicia quando a página é atualizada; não envia emails, SMS nem notificações reais.
- O rótulo **demonstração** mantém-se visível e o comprovativo não tem validade fiscal.

## Jornada demonstrável

1. **Acordo:** rever o artigo e confirmar que se trata de uma demonstração.
2. **Checkout:** escolher cartão fictício ou MB WAY ilustrativo; emular resposta aprovada ou recusada, sem recolher dados.
3. **Resultado:** mostrar resposta simulada, discriminação de valores e ausência de cobrança.
4. **Logística:** anunciante propõe entrega em mãos, ponto de recolha ou transportadora fictícia, com lugar público e data futura; o locatário aceita.
5. **Entrega:** ambas as partes confirmam separadamente, após checklist ilustrativa.
6. **Utilização:** percurso "em curso" e possibilidade de abrir e resolver ocorrência fictícia.
7. **Devolução:** locatário e anunciante validam separadamente; uma ocorrência aberta bloqueia o fecho.
8. **Conclusão:** comprovativo DEMO, histórico e avaliação fictícia.
9. **Alternativa:** após aprovação fictícia e antes de entrega, emular cancelamento e reembolso integral sem movimentos reais.

## Critérios de aceitação

- [ ] O percurso completo termina apenas após acordo, aprovação fictícia, entrega combinada, dupla confirmação e dupla devolução.
- [ ] Aprovação recusada não habilita entrega; existe opção de repetir.
- [ ] Não é permitido confirmar entrega antes de haver proposta aceite.
- [ ] Dados da proposta têm modalidade suportada, local público com 3–120 caracteres e data futura.
- [ ] O mesmo participante não pode confirmar duas vezes.
- [ ] Uma ocorrência aberta impede avançar para devolução ou conclusão.
- [ ] Cancelamento/reembolso são descritos sempre como simulados; nenhuma chave/checkout/webhook da Stripe é invocado.
- [ ] Nenhum dado de pagamento (cartão, IBAN, telefone) é pedido ou persistido.
- [ ] Informações associadas a um pedido só aparecem aos respetivos participantes autenticados.
- [ ] A experiência funciona em ecrãs móveis e com teclado; a marca "demonstração" mantém-se evidente.

## Verificação automatizada

\`\`\`bash
node --check wwwroot/js/payment-demo-flow.js
node --check wwwroot/js/payment-demo.js
node --test tests/DemoFlow/payment-flow.test.js
dotnet restore aluguel.csproj
dotnet build aluguel.csproj --no-restore -c Release
\`\`\`

Os testes Node exercitam resultados recusados, validação do acordo, datas inválidas, confirmação por ambas as partes, incidentes, devoluções e reembolso ilustrativo. A CI executa também as verificações existentes do projeto e o build Docker.

## Stripe test mode (para a integração independente da demonstração)

Já existem \`StripeTestGateway\`, \`StripeWebhooksController\`, \`StripeV2Accounts\` e os endpoints de sandbox do projeto. Para efetivamente utilizar o Stripe test mode **não basta** instalar o SDK: é necessário conectar uma conta Stripe e configurar, de forma privada, as opções exigidas por \`Services/StripePaymentOptions.cs\`, além dos eventos de webhook de teste. Não copiar segredos para ficheiros públicos ou para mensagens.

A demonstração completa funciona sem estas credenciais. A ativação de cobranças reais, comissões, Stripe Connect, impostos e reembolsos operacionais exige onboarding, verificação do operador e revisão financeira e legal. **Nunca substituir estados reais de pagamento por sucessos fictícios**.

## Limitações assumidas

- Não cria contratos vinculativos, pagamentos, reservas, envios, notificações ou avaliações reais.
- Os dois papéis (anunciante/locatário) são clicáveis no mesmo ecrã para apresentação; no fluxo efetivo devem ficar separados por autenticação e autorização no servidor.
- A entrega por transportadora não chama transportadoras nem gera etiqueta ou tracking.
- Não existe persistência, sincronização entre utilizadores nem garantia de disponibilidade durante a demonstração.
- Não usar esta página como evidência de cobranças, entregas, reembolsos ou avaliações efetuados.
