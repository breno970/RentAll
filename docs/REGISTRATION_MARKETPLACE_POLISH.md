# RentAll marketplace enhancement: scope and verification

## Concrete corrections
- Registration checkbox posts a single `AcceptTerms=true` value when selected. The server rejects requests missing that value, rather than relying on browser JavaScript or only the ASP.NET Core check-box tag helper.
- Public catalogue/search cards remain available. `/Anuncios/Details/{id}` redirects unauthenticated users to the account registration flow with the originally requested listing as `returnUrl`. The listing's full description, public profile and messaging are only available after authentication.
- Added accessible, reduced-motion-friendly feedback: password requirement indicator, inline terms state, tasteful card elevation, keyboard focus styling, scroll to top and publish progress. The existing brand, palette and artwork remain unchanged.

## Internal read-only endpoints
- `GET /api/v1/catalogo/categorias` — category IDs + names, no personal information
- `GET /api/v1/catalogo/tipos?categoriaId=123` — types for the selected category (consumed in the form)
- `GET /api/v1/catalogo/regioes?pais=PT|BR` — supported country subdivisions
- `GET /api/v1/catalogo/resumo?pais=PT|BR` — aggregated public counts only
- These APIs do not grant access to private listings or user records. Existing `/healthz` remains the service health endpoint.

## Outside the scope of this patch
- Third-party Google Maps API still requires a correctly restricted browser API key and enabled billing/services. It must not be impersonated with mock responses.
- Payment gateway, bank account/identity verification, insurance, external logistics and AI model inference are not configured. No claims of completion.
- A full end-to-end registration test requires test credentials, cookie handling, a running deployment, and ideally a disposable account, not only a successful compilation.

## UX research
Reference common product patterns, never copy branding:
- OLX Portugal: photo-first ads, clear creation steps, messaging and safe handoffs, https://www.olx.pt/howitworks/
- Vinted: secure messaging, visibility of trust profiles and reviews, https://www.vinted.pt/safety

## Deployment gate
Pass CI (.NET build, JavaScript syntax, Docker image), deploy to Render and verify GET endpoints plus guest redirect before calling the update complete.
