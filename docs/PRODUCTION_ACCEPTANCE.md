# RentAll — critérios de aceitação para produção

> Estado em 9 de novembro de 2026: **plano de validação**, não certificado de produção. Nenhuma caixa deve ser marcada sem evidência. Código analisado na ramificação `recovery/supabase-render`, que é a origem do serviço Render. Não confundir o histórico `Master` com a versão publicada.

## Regras de aprovação

- **P0**: segurança, persistência, autenticação, autorização, integridade de alugueres e pagamentos. Bloqueia lançamento perante falha.
- **P1**: qualidade visual, experiência, acessibilidade e desempenho. Deve ser aceite antes de promover a experiência como comercialmente pronta.
- **P2**: manutenção, refatoração e qualidade de cobertura adicional.
- Estados permitidos: **pendente**, **em teste**, **aprovado com evidência**, **reprovado**, **bloqueado**.
- Toda a alteração deve distinguir `implementado` de `testado`, e `sandbox` de `pagamento real`.

## P0 — Registo, autenticação e recuperação de conta

- [ ] Registo válido cria apenas uma conta, mesmo perante duplo envio.
- [ ] Login inválido não autentica e não revela a existência de contas.
- [ ] Sessão e logout respeitam cookies protegidos e autorização no servidor.
- [ ] Alteração de palavra-passe e recuperação por email funcionam com fornecedor de email configurado.
- [ ] Fluxos de recuperação não são anunciados como funcionais se o envio de email estiver indisponível.
- [ ] Pedidos indevidos são limitados e documentados com respostas seguras.

## P0 — Artigos, fotografias e anúncios

- [ ] Criar artigo: título com 1–140 caracteres não vazios; descrição com 1–6000; validação no servidor.
- [ ] Criar anúncio válido com 1–5 fotografias suportadas (JPG/PNG/WebP, até 5 MB por imagem).
- [ ] Fotografias mantêm a ordem definida, persistem após atualização e não revelam contactos/moradas privadas.
- [ ] Editar e eliminar só é possível ao proprietário autorizado.
- [ ] Uma falha de armazenamento ou BD não deixa imagens sem associação sem tentativa de limpeza.
- [ ] Um erro ao guardar um artigo mostra mensagem sem causar outra falha de base de dados.
- [ ] Categorias/tipos, localização, preço e duração são validados no servidor.
- [ ] Anúncio privado/inativo não aparece publicamente, mesmo por URL direto.
- [ ] Registos associados impedem eliminação destrutiva quando aplicável, com explicação clara.
- [ ] Os filtros respeitam o mercado ativo: Portugal é o mercado atual de publicação. Brasil só deve ser ativado após regras operacionais, tributárias e financeiras.

## P0 — Pedidos, negociação e mensagens

- [ ] Não é possível pedir o aluguer do próprio anúncio.
- [ ] Preço/duração válidos e intervalos permitidos são confirmados no servidor.
- [ ] Pedidos pendentes duplicados são impedidos também perante concorrência.
- [ ] Só interessado e proprietário consultam/alteram o respetivo pedido.
- [ ] Aceitar, recusar e fazer contraproposta obedecem ao estado atual sem duplicar transições.
- [ ] As mensagens estão protegidas e não expõem conversas alheias.
- [ ] O bloqueio de contactos externos apresenta motivo e preserva rascunhos para correção.
- [ ] Erros de rede mostram falha sem declarar envio realizado.

## P0 — Pagamentos, comissões e legalidade

- [ ] A interface distingue sempre **simulação**, **Stripe sandbox** e **cobrança real**.
- [ ] Nenhuma operação de demonstração cobra dinheiro ou solicita dados reais de cartão.
- [ ] Webhooks Stripe verificam assinatura, referência do pedido, montante e moeda.
- [ ] Reentregas do mesmo evento não alteram o saldo duas vezes.
- [ ] Reembolsos e disputas são reconciliados sem tratar disputa como reembolso.
- [ ] Ativar produção **apenas** após verificação Stripe, definição fiscal/operador legal e contratos de marketplace.
- [ ] A comissão apresentada reflete exatamente o estado real do produto e não sugere cobrança já disponível.

## P1 — Interface, UX e identidade RentAll

- [ ] Página inicial, catálogo, anúncio, publicação, painel pessoal, mensagens e pedidos partilham design e tipografia coerentes.
- [ ] O símbolo, marca e conceito RentAll são preservados.
- [ ] Não há botões decorativos, links mortos, texto de preenchimento ou avisos falsos de sucesso.
- [ ] Loading, erro, confirmação e ausência de resultados são claros e acessíveis.
- [ ] Navegação e ações principais funcionam com teclado e em dispositivos táteis.
- [ ] Com 320, 375, 768, 1024 e 1440 px não existem sobreposições ou scroll lateral inesperado.
- [ ] Componentes mostram o estado financeiro correto e não ocultam que o checkout ainda é demonstrativo.
- [ ] Estados/países/regiões são consistentes em pesquisa e publicação, sem prometer operação no Brasil antes de estar ativa.

## P1 — Pesquisa e desempenho

- [ ] Pesquisa, filtros e ordenação combinados devolvem resultados corretos, com dados reais.
- [ ] Páginas vazias e erros não eliminam filtros nem deixam o utilizador sem orientação.
- [ ] Listagens extensas têm paginação ou carregamento incremental previsível.
- [ ] Imagens e scripts não bloqueiam desnecessariamente a renderização.
- [ ] Medir e acompanhar LCP <= 2,5 s, INP <= 200 ms e CLS <= 0,1 no percentil 75 de dados reais, quando disponíveis.

## P0 — Infraestrutura e aceitação final

- [ ] Pipeline compila, valida JavaScript e executa testes de regressão; Docker faz build.
- [ ] Segredos presentes só nos gestores de configuração; nenhum valor sensível em Git.
- [ ] `/healthz` verifica conectividade BD sem divulgar detalhes.
- [ ] Login, sessões e dados mantêm-se coerentes após reinício do Render.
- [ ] Monitorização e recuperação para erros críticos têm responsáveis e procedimento escrito.
- [ ] Backups e recuperação da base de dados são ensaiados em ambiente seguro.
- [ ] Browser e testes E2E abrangem registo, publicação, pesquisa, pedido, mensagens e acesso indevido.
- [ ] Nenhum deploy para a versão pública sem revisão das alterações, evidências e plano de rollback.

## P2 — Engenharia

- [ ] Remover código não utilizado apenas após análise de referências e testes.
- [ ] Componentes partilhados têm estilos e estados reutilizáveis.
- [ ] Não adicionar dependências desnecessárias.
- [ ] Documentar decisões arquiteturais, migrações e obrigações ainda por cumprir.

## Modelo de evidência por funcionalidade

| Área/funcionalidade | Estado | Teste/comando | Evidência (PR, log, screenshot) | Riscos pendentes |
| --- | --- | --- | --- | --- |
| Validação de títulos e descrições | Em teste | `dotnet run --project tests/ContactSafetyChecks/ContactSafetyChecks.csproj -c Release` | CI da PR deste trabalho | Verificar formulário e persistência em staging |
| Editar artigo quando a BD falha | Em teste | Cenário manual de falha de gravação | Rever resultado em staging | Simular indisponibilidade sem afetar produção |
| Stripe pagamentos reais | Bloqueado | Não aplicável | Plataforma permanece limitada a teste/sandbox | Requisitos fiscais/operacionais e ativação de conta |

**Definition of Done:** compilação e testes relevantes aprovados + permissões e casos negativos verificados + experiência responsive revista + evidência de staging + responsável aprova. Se faltar uma dessas provas, estado continua pendente.
