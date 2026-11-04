# RentAll — controlos iniciais e pendências RGPD/LGPD/DSA

## Entregue nesta branch
- Uma única publicação com artigo/anúncio e imagens (até 5, JPG/PNG/WebP).
- Morada exata, contacto e código postal deixam de ser recolhidos no novo fluxo; a API de localização só atua após ação do titular.
- Imagens são reprocessadas no navegador antes do envio para retirar metadados EXIF e GPS; o serviço não deve confiar nisto como garantia absoluta, pelo que é necessária validação adicional de conteúdo no servidor.
- Perfis públicos limitados a alcunha/nome de anúncio, data de entrada para novas contas e anúncios disponíveis. Histórico não é rotulado incorretamente como antiguidade de registo.
- Mensagens entre participantes ligadas ao anúncio, sem dados de contacto visíveis, e rate limit para prevenção de spam.
- Cabeçalhos de segurança e CSRF nos formulários existentes; credenciais permanecem fora do repositório.
- Bucket público de fotos: **nunca deve receber conteúdo privado**.

## Exigências ainda por implementar antes de uma operação comercial plena
1. Identificar formalmente o controlador, morada profissional de contacto e, quando aplicável, representante/encarregado; inventário e registo dos tratamentos.
2. Bases legais por finalidade, transparência detalhada (art. 13 RGPD, art. 9 LGPD), política de conservação, critérios de eliminação e resposta verificável a pedidos dos titulares (acesso, retificação, eliminação, portabilidade).
3. Avaliar transferência de dados entre UE/Brasil e prestadores cloud, contratos de tratamento, medidas técnicas, restrições a menores e eventual avaliação de impacto (AIPD/DPIA).
4. Segurança: rotação de chaves expostas no histórico/conversas, gestão de incidentes, backups com restauro testado, logs de auditoria, retenção limitada, análise de malware e controlo de conteúdos, revalidação de uploads e DNS/API key restrictions.
5. Moderadores: denunciar anúncios/conversas, decisões justificadas, recursos e combate a bens ilegais ou anúncios fraudulentos, em atenção ao Digital Services Act europeu e às regras brasileiras aplicáveis. Rever a distinção entre locadores profissionais e particulares antes de recolher dados de verificação de comerciantes.
6. Cookies e serviços de terceiros: Google Maps só por opt-in e com aviso claro; cookies não essenciais de análise/marketing só após gestão de consentimento adequada quando exigível.
7. Pagamentos ainda não lançados: parceiro regulado com KYC, fraude, chargebacks, políticas de cancelamento, transparência de taxas, obrigações fiscais.
8. Rever e atualizar os termos de utilização, avisos de privacidade e contratos por jurisdição antes de considerar conformidade final.

## Limitações importantes
- A deteção via Google Maps requer chave **BrowserApiKey** válida, com faturação e APIs corretas habilitadas e restrições HTTP referrer. Sem isso, zona manual e link externo funcionam, mas deteção automática não está disponível.
- Chamadas geocoder podem enviar localização a terceiros após consentimento do navegador e ação do utilizador; o navegador pode limitar permissões.
- O filtro de e-mail/telefone em chat é imperfeito e deve ser combinado com moderação.
- Fotografias antigas podem conter EXIF e anúncios históricos podem ter dados que os formulários novos não recolhem. É necessário procedimento separado de limpeza e revisão.
- Revisão jurídica profissional é necessária; código não equivale a certificação de conformidade.

Fontes: https://eur-lex.europa.eu/eli/reg/2016/679/pt ; https://www.gov.br/anpd/pt-br/assuntos/titular-de-dados-1/direito-dos-titulares ; https://eur-lex.europa.eu/legal-content/EN/TXT/?uri=CELEX:32022R2065 .
