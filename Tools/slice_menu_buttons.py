"""Fatia menu_buttons_sheet.png em sprites por botão e estado."""
from __future__ import annotations

from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SHEET = ROOT / "Assets" / "UI" / "Menu" / "menu_buttons_sheet.png"
OUT = ROOT / "Assets" / "UI" / "Menu" / "Buttons"

# Grid 2×4 (1024×682). (linha, coluna) → nome do arquivo.
SLICES = {
    (0, 0): "jogar_selected.png",
    (0, 1): "opcoes_hover.png",
    (1, 0): "jogar_hover.png",
    (1, 1): "creditos_hover.png",
    (2, 0): "jogar_normal.png",
    (2, 1): "opcoes_normal.png",
    (3, 0): "creditos_normal.png",
    (3, 1): "sair_normal.png",
}


def main() -> None:
    if not SHEET.exists():
        raise SystemExit(f"Não encontrado: {SHEET}")

    sheet = Image.open(SHEET).convert("RGBA")
    w, h = sheet.size
    cols, rows = 2, 4
    cell_w, cell_h = w // cols, h // rows
    OUT.mkdir(parents=True, exist_ok=True)

    for (row, col), name in SLICES.items():
        l = col * cell_w
        t = row * cell_h
        r = w if col == cols - 1 else l + cell_w
        b = h if row == rows - 1 else t + cell_h
        crop = sheet.crop((l, t, r, b))
        crop.save(OUT / name)

    # SAIR não tem hover/selected na folha — reutiliza moldura genérica.
    (OUT / "sair_hover.png").write_bytes((OUT / "creditos_hover.png").read_bytes())

    print("Sprites em", OUT)
    for p in sorted(OUT.glob("*.png")):
        print(" -", p.name)


if __name__ == "__main__":
    main()
