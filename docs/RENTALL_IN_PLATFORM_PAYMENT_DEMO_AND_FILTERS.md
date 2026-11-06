# RentAll — Proteção da negociação, pagamento demonstrativo e filtros PT/BR

## Implementação
- Controlo preventivo server-side em textos públicos e mensagens: telefones com separadores, unicode invisível e números por extenso, e-mails disfarçados, redes sociais, domínios e pedidos de pagamento externo. Não lê imagens nem garante ausência de evasão.
- A mensagem recusada não é guardada nem enviada. Uma mensagem legítima pode ser corrigida mantendo o rascunho; há aviso no navegador apenas como ajuda. Verificações automáticas e contactos para contestação declarados nos termos e política de privacidade.
- Pedidos de aluguer permitem uma contraproposta estruturada por preço/dia e dias. O estado `Contraproposta` não substitui os termos originais até a outra parte aceitar. A outra parte pode recusar, ou o proponente retirar. As respostas e o cancelamento têm proteção de concorrência e verificações de autorização.
- Estados e ofertas da mesma pessoa por artigo mantêm exclusividade de negociação ativa por índice parcial. Não há transação bancária, depósito ou reserva paga nesta funcionalidade.
- Página `/Planos`: publicação gratuita; modelo indicativo de comissão de 10% por aluguer pago; ferramentas Pro e destaque como projetos futuros **não disponíveis**.
- Página `/Pagamentos/Demonstracao`: quatro ecrãs locais — pedido, seleção do método (cartão fictício ou MB WAY/Pix de exemplo), autorização aprovada/recusada fictícia e comprovativo sem validade fiscal mais processo de entrega, devolução e avaliação ilustrativo. O navegador não pede número de cartão ou dados pessoais nem envia dados financeiros. O backend limita-se a consultar o resumo de um pedido se o utilizador autenticado for um participante.
- Filtros: Portugal como país inicial; Brasil selecionável; os códigos de distrito/estado dependem do país e atualizam as moedas. Sugestões de cidades passam a vir dos anúncios ativos da RentAll via `GET /Home/Cidades`, sem enviar automaticamente dados de filtros a um serviço externo. Cidade continua a aceitar texto manual. A paginação preserva os filtros.
- Testes de regressão no GitHub Actions para contactos, país/regiões e limites de preços; compilações Razor, .NET, JS e Docker.

## Bloqueios comerciais
A comissão de 10% **não é cobrada**. Não existe venda de destaque, Pro ativo, seguro, garantia de aluguer ou escrow; esses serviços exigem desenho fiscal, termos, proteção ao consumidor e verificação bancária. Não chamar a demonstração de pagamento, reserva ou recibo real.

## Limitações e testes a fazer num navegador
- Testar chat com duas contas, números e links ocultos, falsas deteções e pedidos associados, antes de anunciar que o sistema é infalível.
- Testar proposta, aceitação, rejeição e cancelamento simultâneo por duas contas em mobile e desktop; os estados concorrentes devem ser corretamente tratados.
- Validar RGPD, prazos de retenção, identificação do operador e regras de moderação antes de exploração comercial em Portugal.
- Testar cidades PT (Lisboa, Porto, etc.) e UFs BR, mudança de país após filtros preenchidos, datalist sem anúncios, pesquisa sem JavaScript e erros de rede.
- Verificar no separador Rede que o checkout demonstrativo não envia dados para PSP, e que está claramente identificado como demonstração em todas as etapas.

## Próxima fase de faturação real
1. Formalização como pessoa singular em Portugal e identificação legal no site.
2. Abrir/ligar sandbox Stripe e verificar contas conectadas.
3. Completar e testar pagamentos, disputas, reembolsos e responsabilidades fiscais, incluindo Stripe Tax.
4. Ativar receitas somente depois de consentimento e contratos/termos adequados, sem prometer garantias não existentes.
