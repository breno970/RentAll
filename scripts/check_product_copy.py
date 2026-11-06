#!/usr/bin/env python3
"""Check that customer-facing pages present a truthful, finished-product experience."""
from pathlib import Path

pages = [
    "Views/Home/Index.cshtml",
    "Views/Experiencia/Index.cshtml",
    "Views/Anuncios/Details.cshtml",
    "Views/Pedidos/Index.cshtml",
    "Views/Publicar/Index.cshtml",
    "Views/Planos/Index.cshtml",
    "Views/Shared/_Layout.cshtml",
]
for path in pages:
    content = Path(path).read_text(encoding="utf-8").casefold()
    assert "protótipo" not in content and "prototipo" not in content, (
        f"Development-stage label returned to public-facing {path}"
    )

journey = Path("Views/Experiencia/Index.cshtml").read_text(encoding="utf-8").casefold()
for required in ["mensagens", "pedidos", "cauç", "em preparação", "não há cobranças", "incidentes"]:
    assert required in journey, f"Rental journey missing important feature/status: {required}"

payment = Path("Views/Pagamentos/Demonstracao.cshtml").read_text(encoding="utf-8").casefold()
for required in ["simulada", "não cobra", "não recolhe dados bancários", "sem validade fiscal"]:
    assert required in payment, f"Payment preview missing crucial no-payment disclosure: {required}"

controller = Path("Controllers/ExperienciaController.cs").read_text(encoding="utf-8")
assert '[HttpGet("/como-funciona")]' in controller
assert "RedirectToActionPermanent" in controller

print("Production-facing copy, payment transparency, and friendly URL checks passed.")
