#!/usr/bin/env python3
"""Guard the shared RentAll layout and Razor markup against regressions."""
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parent.parent
files = list((root / "Views").rglob("*.cshtml")) + list((root / "Areas/Identity/Pages").rglob("*.cshtml"))
issues = []

for path in files:
    relative = path.relative_to(root).as_posix()
    data = path.read_text(encoding="utf-8-sig")
    if relative == "Views/Shared/_Layout.cshtml":
        continue
    if re.search(r"<!DOCTYPE\s+html|<html\b|<head\b|<body\b", data, re.I):
        issues.append(f"{relative}: contains a duplicate HTML document instead of using the layout")
    if len(re.findall(r"<form(?:\s|>)", data, re.I)) != len(re.findall(r"</form\s*>", data, re.I)):
        issues.append(f"{relative}: unbalanced form tags")
    if "footer-container" in data and relative != "Views/Shared/_Layout.cshtml":
        issues.append(f"{relative}: legacy duplicated footer remains")

layout = (root / "Views/Shared/_Layout.cshtml").read_text(encoding="utf-8-sig")
for required in ('~/imagens/ra.png', '~/imagens/ico.png', '~/css/rentall-classificados.css', 'class="site-footer"'):
    if required not in layout:
        issues.append(f"Layout missing {required}")

if not (root / "wwwroot/css/rentall-classificados.css").is_file():
    issues.append("Main CSS file missing")
if not (root / "wwwroot/js/rentall-ui.js").is_file():
    issues.append("Main interaction script missing")

if issues:
    print("\n".join(issues), file=sys.stderr)
    raise SystemExit(1)
print(f"Validated {len(files)} Razor files, global branding and shared responsive assets.")
