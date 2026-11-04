# RentAll — percurso de aluguer e retenção na plataforma

## O que muda
- A página da conversa liga diretamente o chat ao pedido de aluguer correspondente, mostra o estado e deixa criar um pedido (quando o anúncio tem preço) sem sair da conversa.
- Respostas rápidas facilitam perguntas de disponibilidade, entrega em local público e estado do artigo, sem exigir contactos.
- A página Pedidos oferece um atalho para a conversa correspondente, desde o primeiro pedido, mesmo antes da primeira mensagem.
- A camada ASP.NET impede novos anúncios com telefone, links, email, utilizadores @ e referências explícitas a apps externas, tanto na publicação unificada como nos caminhos legados de itens e anúncios. O mesmo controlo é aplicado ao envio de mensagens.
- Os formulários legados deixam de pedir contactos que nunca eram efetivamente guardados.
- CSS editorial compacto e responsivo em chats e pedidos.

## Monetização e transparência
- O futuro modelo de receita é comissão de 10% sobre alugueres com pagamento confirmado dentro da RentAll. **A comissão ainda não é cobrada**, pois o Stripe está desligado e não há contas conectadas testadas.
- Não anunciar seguro, escrow, proteção financeira nem devoluções garantidas como funcionalidades ativas.
- Não bloquear a informação útil ou impedir local de entrega; incentivar os acordos dentro da RentAll oferecendo histórico, pedidos e apoio de moderação.
- Comunicar a distinção entre contactos voluntariamente trocados em encontros presenciais e desviar negociações para aplicativos externos. As barreiras de texto reduzem mas não eliminam desvios.
- Estratégia posterior: pagamentos reais auditados, confirmação de entrega/devolução, avaliações apenas após aluguer concluído verificado, promoções de anúncios opcionais e suporte especializado.

## Testes e riscos
- Testar conversas iniciadas antes e depois do pedido, anúncio desativado com pedido existente, criação de pedido sem mensagens, dono e interessado com contas diferentes.
- Testar tentativas de publicar emails, URLs, números de telefone, nomes de apps externas, contornos deliberados e textos legítimos (como modelos de produtos com números).
- A verificação de conteúdos é regex sem OCR de fotografias, não substitui revisão humana ou denúncias.
- Os novos mecanismos não alteram os registos financeiros ou as migrações e não ativam pagamentos.
- Recomenda-se acompanhar a taxa visita→conversa→pedido→pagamento apenas depois de garantir consentimentos e minimização de dados.

## Referências de práticas de segurança
- https://ajuda.olx.com.br/s/article/chat-boas-praticas
- https://www.vinted.pt/safety
