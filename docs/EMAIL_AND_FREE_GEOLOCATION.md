# RentAll — envio de emails e localização aproximada

## Email transacional (ASP.NET Identity + SendGrid)

A aplicação requer duas variáveis privadas no Render, ambas configuradas e válidas:
- `SendGridKey`: API key criada no SendGrid com permissão para envio de email.
- `EmailFromAddress`: endereço ou domínio **verificado** em SendGrid Sender Authentication.
- Opcional: `EmailFromName` (por omissão RentAll).

Sem estas duas configurações, o código não deve afirmar que envia emails. A confirmação de contas só é exigida quando ambas são fornecidas; com fornecedor configurado o registo entrega um link de confirmação. O remetente deixou de ser fixo no código.

Fluxos: confirmar email na criação de conta, reenviar confirmação e redefinir palavra-passe. As mensagens usam estilos próprios da RentAll, links gerados pelo próprio servidor e sem tracking de clique/abertura. O serviço regista apenas códigos HTTP/status do prestador, nunca tokens. Limite anti-abuso: 5 pedidos por IP em 15 minutos. O token de validação tem duração máxima de 3 horas.

**Validar antes de prometer entrega:** domínio/remetente verificado, SPF/DKIM, API key, quotas, receção na caixa de entrada e no spam, expiração do token, redefinição e login da conta confirmada. Aceitação HTTP 202 pelo SendGrid não garante entrega final.

## Localização gratuita

Fornecedor: BigDataCloud free client-side reverse geocoding, `https://api.bigdatacloud.net/data/reverse-geocode-client`. **Não requer API key**, mas tem condições de utilização.

- O pedido só é enviado após clique e autorização expressa de geolocalização do navegador, e sai diretamente do browser.
- A BigDataCloud recebe coordenadas e IP e pode usar esses dados para os seus serviços de precisão, conforme os termos publicados. A aplicação não grava latitude/longitude nem as envia ao nosso servidor.
- Países suportados: PT e BR; a região é comparada com catálogo interno; município/cidade pode exigir confirmação manual.
- Sem coordenadas, não há fallback automático por IP: em caso de recusa, usa-se a seleção manual.
- A pré-visualização opcional do Google Maps é separada e continua dependente das credenciais do Google quando utilizada.

Referência de condições: https://www.bigdatacloud.com/docs/free-api/free-reverse-geocode-to-city-api

## Testes mínimos

1. Abrir /Publicar em HTTPS, clicar no botão, aprovar localização e confirmar país/região/cidade; testar recusa, falha de GPS e região não encontrada.
2. Verificar no DevTools que não há chamadas a BigDataCloud antes do clique e que a requisição é client-side.
3. Confirmar que o POST não contém latitude/longitude e que a base de dados conserva apenas cidade e região.
4. Não publicar contas e emails de teste em ambiente público sem consentimento/caixa de correio controlada.
