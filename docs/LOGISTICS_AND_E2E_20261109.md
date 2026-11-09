# Logística de entrega/devolução e verificação E2E (09/11/2026)

## Implementado
- Histórico partilhado (`rentall."PercursoDemoEventos"`, migração **014**, aditiva e idempotente): cada etapa regista autor, papel e hora.
- Locatário pode **pedir alterações** à proposta de entrega (com motivo); o anunciante volta a propor.
- **Checklist** de entrega e de devolução: estado do artigo, acessórios, observações e fotografia opcional (bytes validados).
- **Divergência na devolução** (anunciante) suspende a conclusão até ser resolvida; ocorrências com descrição.
- Resumo do pedido (preço/dia, duração, total, comissão indicativa, valor para o anunciante, cobrado 0,00 €), etapas de progresso e **comprovativo demonstrativo** imprimível, marcado "sem valor fiscal".
- Sem a migração 014 aplicada, o percurso continua a funcionar sem as funções de histórico (verificado).

## Correções
- Eliminar anúncio/artigo com alugueres em aberto apagava em cascata os pedidos, mensagens e percursos da outra parte → agora bloqueado com sugestão de ocultar.
- Limitador partilhado (10 ações/10 min) interrompia o percurso completo com HTTP 429 em branco → política própria `rental-journey` + página de erro legível.
- Publicação: corrida entre a sugestão automática de categoria e a escolha manual misturava tipos de categorias → pedidos obsoletos cancelados.
- Uploads aceitavam qualquer conteúdo com `Content-Type` de imagem → assinatura JPEG/PNG/WebP verificada.
- Duplo clique repetia operações → `data-submit-lock` ignora reenvios.
- `Pagamentos/Reautorizar` sem tratamento de erro Stripe.
- PR #16 (limites de título/descrição) integrada na linha de produção.

## Como correr o E2E
PostgreSQL local com `database/*.sql`, app com `ASPNETCORE_URLS=http://localhost:5080`, storage simulado; depois
`node tests/E2E/rental-journey.e2e.js` (Playwright). Cobre 3 contas (anunciante, locatário, intruso).

## Publicação
1. Aplicar `database/014_rental_demo_logistics_events.sql` no Supabase (SQL editor). Rollback: `DROP TABLE rentall."PercursoDemoEventos";`
2. Fazer merge para `recovery/supabase-render` e Manual Deploy no Render. Rollback: redeploy do commit `3144c42`.
Ordem indiferente: o código tolera a tabela em falta.
