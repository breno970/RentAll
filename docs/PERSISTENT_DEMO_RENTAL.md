# RentAll — aluguer demonstrativo persistente (duas contas)

## O que funciona

O utilizador interessado e o anunciante iniciam sessão **nas respetivas contas** e consultam o mesmo pedido de aluguer aceite. A página \`/PercursoDemo/{pedidoId}\` apresenta:

1. **Checkout demonstrativo**: o locatário escolhe cartão/MB WAY fictícios e simula aprovação ou recusa. A recusa permite repetir sem qualquer cobrança.
2. **Proposta de entrega**: o anunciante escolhe modalidade, ponto de encontro e data/hora de Portugal; o locatário aceita. A proposta pode ser corrigida antes de aceite.
3. **Dupla confirmação**: o anunciante marca a entrega e o locatário regista a receção na sua conta. Apenas após ambas o aluguer passa a "Em curso".
4. **Devolução**: o locatário regista a devolução; o anunciante confirma a receção e encerra o percurso.
5. **Incidentes**: uma ocorrência demonstrativa impede devolução/fecho até ser resolvida; a resolução é marcada pelo anunciante.
6. **Conclusão**: o locatário atribui avaliação 1–5 (simulada e não publicada no perfil).
7. **Cancelamento alternativo**: antes de entrega acordada, o locatário pode demonstrar cancelamento/reembolso **fictícios**, sem dinheiro movimentado.
8. **Persistência**: estados e datas ficam na tabela privada \`rentall."PercursosAluguerDemo"\` e continuam após atualizar a página; ambos os participantes consultam o mesmo estado.

## Condições de segurança

- \`[Authorize]\`, filtro pelo ID da conta autenticada, antiforgery em todos os POST e rate limiting.
- Cada operação revalida se o pedido permanece aceite e se a pessoa tem o papel correto.
- \`Versao\` é um token de concorrência para rejeitar alterações simultâneas.
- Não guarda dados bancários, telemóveis, moradas completas nem tokens Stripe.
- O fluxo **não escreve em** \`PagamentosAluguer\`, nem chama Stripe, bancos, transportadoras, email ou notificações externas.
- A tabela está isolada no esquema privado \`rentall\` e só dá acesso SQL ao backend \`rentall_app\`; \`anon\` e \`authenticated\` não têm privilégios.
- A página indica sempre que os estados representam **demonstração** e que o valor cobrado é **0,00 €**.
- O checkout Stripe sandbox real continua independente e requer conta/credenciais de teste; não confundir os dois.
- As datas são convertidas explicitamente do horário de Lisboa para UTC no servidor; não se aceitam datas passadas.

## Critérios de aceitação

- [ ] Dois utilizadores autenticados conseguem acompanhar o mesmo pedido aceite.
- [ ] O locatário inicia, aprova/recusa e, se necessário, repete checkout sem cobrança.
- [ ] O anunciante não consegue simular pagamento, e o locatário não consegue propor entrega.
- [ ] A proposta exige modalidade válida, local de 3–120 caracteres e data entre 15 min e 90 dias no futuro.
- [ ] A entrega só é acordada pelo locatário e exige confirmação de ambas as partes.
- [ ] A devolução só pode ser iniciada pelo locatário depois da entrega; o anunciante confirma a receção.
- [ ] Não é possível concluir com incidente aberto, cancelar depois da entrega acordada nem avaliar duas vezes.
- [ ] Utilizador alheio ao pedido recebe 404 para GET/POST.
- [ ] Duas alterações simultâneas não corrompem o estado.
- [ ] Atualizar página ou sair/entrar não perde o estado guardado.
- [ ] O build, Docker, testes puros e fluxos de navegador passam.
- [ ] Stripe não é invocada neste módulo, mesmo que existam chaves de teste configuradas.

## Migração e deploy

1. Confirmar o estado e privilégios da base de dados na Supabase.
2. Aplicar **uma única vez** \`database/013_persistent_rental_demo.sql\` na base de dados do projeto RentAll, antes da publicação do backend.
3. Verificar existência, colunas, grants e RLS da nova tabela **sem consultar dados dos utilizadores**.
4. Publicar a versão validada pela CI no serviço Render que acompanha \`recovery/supabase-render\`.
5. Confirmar \`/healthz\` e acesso com duas contas de demonstração próprias; não testar com transações reais.

## Testes automatizados

\`\`\`bash
dotnet run --project tests/PercursoDemoChecks/PercursoDemoChecks.csproj -c Release
dotnet build aluguel.csproj -c Release
node --test tests/DemoFlow/payment-flow.test.js
\`\`\`

O teste puro verifica regras, transições e permissões. Os testes de integração com Postgres real, sessão, CSRF e navegação visual continuam a ser exigidos para aceitar totalmente a funcionalidade.
