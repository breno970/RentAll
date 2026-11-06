# RentAll — revisão jurídica e de consentimento (Portugal)

## Aplicado no código
- Política de cookies, com opções claras **Aceitar opcionais / Rejeitar opcionais / Personalizar** e botão de alteração no rodapé.
- Sem carregar Google Fonts ou Font Awesome até haver consentimento explícito. O consentimento opcional é guardado em armazenamento local; se for retirado, a página é recarregada, impedindo novos carregamentos por esta implementação.
- Cookies técnicos de autenticação e antifalsificação configurados como HttpOnly e Secure para HTTPS.
- Google Maps apenas após clique adicional que informa da comunicação de dados técnicos ao fornecedor. BigDataCloud continua a exigir ação e permissão do navegador.
- Novo texto das páginas **Termos, Privacidade, Cookies e Informação legal**, com direitos de proteção de dados, moderação, reclamações, vias RAL e regras de competência sem foro exclusivo incompatível com normas imperativas.
- Demonstração de cartão com número mascarado **não editável**, nenhum formulário de dados bancários, nenhum pedido de rede, nenhum identificador Stripe e nenhuma alteração ao estado do pedido.
- Remoção de rótulos promocionais desnecessários e de uma mensagem de login enganadora no catálogo.
- Retirados do rodapé antigo número, WhatsApp e morada de uma localização brasileira que podiam ser confundidos com contactos profissionais portugueses do operador.

## Questões que exigem dados/autorização do responsável — não concluídas
1. Identificar o **responsável pelo tratamento / prestador**: nome legal, morada profissional e NIF apropriados; completar a identificação pública exigida pelo artigo 10.º do DL 7/2004.
2. Confirmar o início de atividade fiscal e o regime de impostos antes de cobrar comissões.
3. Definir, documentar e implementar prazos reais de retenção, pedidos de apagamento, exportação e correção de dados, gestão de incidentes e eventuais obrigações de comunicação à CNPD.
4. Verificar contratos/DPA e localizações reais de Render, Supabase, BigDataCloud, SendGrid, Google, Font Awesome e Stripe; verificar mecanismos internacionais de transferência e riscos de acesso.
5. Testar de ponta a ponta cookies reais com ferramentas de rede em navegadores desktop/mobile. O banner é uma medida técnica, **não uma certificação**; escolher uma plataforma CMP certificada/certificada não é automaticamente necessário, mas o mecanismo deve conseguir demonstrar o consentimento conforme as obrigações relevantes.
6. Caso aplicável, realizar inscrição do operador no **Livro de Reclamações Eletrónico** e indicar a entidade RAL competente para o serviço.
7. Avaliar obrigações do Regulamento dos Serviços Digitais para plataforma com conteúdo de utilizadores (notificação/ação, decisões, recurso e transparência).
8. Rever os termos com aconselhamento jurídico português e validar a operação destinada ao Brasil separadamente.
9. Reavaliar cookies de serviços de publicidade, análise e conteúdos externos caso sejam introduzidos; não ativar esses scripts sem consentimento específico.
10. Definir condições de cancelamento, entrega, devolução, responsabilidade e proteção do consumidor antes de existir qualquer cobrança de cartão.

## Transparência financeira
O botão **Ver confirmação de exemplo** não constitui compra nem pagamento. Não recolhe número real de cartão, CVV ou morada de faturação. Não pode tornar-se um processamento de pagamentos sem integração segura do PSP, webhook, entidades tributárias e verificação de contas conectadas.

## Fontes legais para revisão
- RGPD: https://eur-lex.europa.eu/eli/reg/2016/679/oj
- Lei 58/2019: https://diariodarepublica.pt/dr/detalhe/lei/58-2019-123815982
- Lei 41/2004 (artigo 5.º): https://diariodarepublica.pt/dr/detalhe/lei/41-2004-480710
- DL 7/2004 (informação sobre prestador): https://diariodarepublica.pt/dr/detalhe/decreto-lei/7-2004-240775
- Decreto-Lei 156/2005 (Livro de Reclamações): https://diariodarepublica.pt/dr/legislacao-consolidada/decreto-lei/2005-34431675
- Regulamento Serviços Digitais: https://eur-lex.europa.eu/eli/reg/2022/2065
- Direitos RGPD: https://www.cnpd.pt/cidadaos/direitos/
- RAL em Portugal: https://www.asae.gov.pt/perguntas-frequentes1/area-economica/resolucao-alternativa-de-litigios-de-consumo.aspx
