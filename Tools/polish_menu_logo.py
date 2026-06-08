"""Torna o preto da logo transparente (menu_logo.png)."""
from pathlib import Path

from PIL import Image

LOGO = Path(__file__).resolve().parents[1] / "Assets" / "UI" / "Menu" / "menu_logo.png"


def main() -> None:
    img = Image.open(LOGO).convert("RGBA")
    px = img.load()
    w, h = img.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if r < 40 and g < 40 and b < 40:
                px[x, y] = (r, g, b, 0)
    img.save(LOGO)
    print("Logo atualizada:", LOGO)


if __name__ == "__main__":
    main()
