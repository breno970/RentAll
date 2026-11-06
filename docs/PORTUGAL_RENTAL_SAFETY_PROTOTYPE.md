# RentAll — Protótipo Portugal: segurança, devolução e monetização

## Implementado (Novembro de 2026)

### Portugal apenas
- Pesquisa pública fixa no mercado PT/EUR, distritos portugueses; chamadas `Pais=BR` ao catálogo são normalizadas para Portugal.
- A API de sugestões de cidades `/Home/Cidades` recusa países diferentes de Portugal.
- Formulários unificados/legados de novos anúncios mostram apenas Portugal; o servidor rejeita novas publicações com outro país.
- Os anúncios históricos brasileiros não são eliminados e deixam de ser visíveis a visitantes por links diretos. A edição dos anúncios históricos BR fica suspensa para impedir alterações acidentais.
- Novos pedidos de aluguer só aceitam anúncios portugueses.
- Registos privados históricos de pedidos e mensagens continuam conservados aos participantes, para não perder a história.

### Conversão e confiança
- Detalhes de anúncios públicos podem ser consultados sem criar conta. Mensagens e pedidos continuam a exigir login.
- Nova página `/Experiencia`, sem login, ligada à navegação, home, detalhes, pedidos, publicação e preços.
- Seis etapas interativas: verificação, condições (preço/dias/valor), pagamento com comissão de 10% e caução separada, entrega com checklist, devolução com checklist, resolução.
- Quatro cenários ilustrativos de devolução: sem incidentes, dano, atraso, não devolução. A saída explica prova, direito de resposta, análise humana, e que crimes devem ser reportados às autoridades.
- Simulador de receita bruta mensal por número de alugueres e comissão, excluindo taxas, impostos e despesas.
- Ações não usam serviços de pagamento, não pedem documentos ou fotos reais, não persistem dados e não confirmam uma reserva, disputa ou pagamento.

## Modelo de receitas futuro
1. Publicação básica gratuita para construir oferta.
2. Comissão indicativa de **10% incluída no preço**, apenas sobre transações confirmadas na plataforma depois da ativação financeira.
3. Futuro destaque facultativo, claramente marcado como publicidade, com preço/termos transparentes.
4. Futuras ferramentas Pro opcionais para anunciantes profissionais, sem afetar indevidamente a visibilidade de anúncios gratuitos.
5. Serviços logísticos/inspeção opcionais com parceiros sob contrato, somente após verificar custos, responsabilidade e conformidade.

## Proteção a implementar para existir na prática
- Identificação de partes de forma proporcional ao risco e verificação pela Stripe/PSP ou prestador KYC, nunca armazenamento informal de documentos na RentAll.
- Confirmação do direito de alugar artigos de elevado valor (e.g. referências/serial com acesso limitado).
- Contrato digital com condições, valor, durações, caução, desgaste aceitável, regras de cancelamento, entrega/devolução e forma de contestar decisões.
- Caução só se o PSP suportar autorizações e captura no período exigido; valores e prazos claramente definidos. Não é garantia de recuperação do artigo nem seguro.
- Fotografias, acessórios e estado antes/depois com consentimento, retenção mínima e acesso limitado aos participantes e à equipa autorizada. Evitar geolocalização exata.
- Confirmação de entrega por ambas as partes; não entregar um artigo apenas porque alguém mostra um ecrã de checkout simulado.
- Fluxo de atrasos, não devolução e dano com notificações, prova, revisão humana, direito de contestação e eventual orientação para PSP e autoridades. Não confiscar cauções automaticamente.
- Revisão das obrigações RGPD/DSA e consumo em Portugal e definição do responsável legal, impostos, faturação, seguros reais se oferecidos.

## Limitações de teste
- CI valida estrutura Razor, JavaScript, compilação e Docker; não certifica visual em navegadores reais ou segurança total.
- Precisam de ser testados em mobile, sem JS e com contas separadas: detalhe público, publicação PT, pedido e contraproposta; histórico BR não exposto; mudança de cidade; simulação de incidentes.
- Impedir todos os desvios para contactos externos é tecnicamente impossível. A plataforma deve oferecer valor real (histórico, confiança, resolução) e transparência, além de moderação proporcional.

## Lançamento financeiro
Manter **StripePayments__Enabled=false** e **StripePayments__TaxEnabled=false** até atividade fiscal, conta Stripe verificada, sandbox e testes E2E concluídos.
