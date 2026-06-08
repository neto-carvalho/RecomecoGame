"""Recorta logo/botões e gera fundo limpo a partir de menu_background.png."""
from __future__ import annotations

from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
MENU = ROOT / "Assets" / "UI" / "Menu"
SOURCE = MENU / "menu_background.png"
PREVIEW = MENU / "_preview"

# Caixas calibradas para a arte em 1024×576.
REGIONS_1024 = {
    "logo": (70, 28, 954, 268),
    "btn_jogar": (292, 272, 732, 318),
    "btn_opcoes": (292, 326, 732, 372),
    "btn_creditos": (292, 380, 732, 426),
    "btn_sair": (292, 434, 732, 480),
}


def scale_box(box: tuple[int, int, int, int], w: int, h: int) -> tuple[int, int, int, int]:
    sw, sh = 1024, 576
    l, t, r, b = box
    return (
        int(l * w / sw),
        int(t * h / sh),
        int(r * w / sw),
        int(b * h / sh),
    )


def center_anchor(box: tuple[int, int, int, int], w: int, h: int) -> tuple[float, float]:
    l, t, r, b = box
    return ((l + r) * 0.5 / w, (t + b) * 0.5 / h)


def button_size_ref(box: tuple[int, int, int, int]) -> tuple[float, float]:
    l, t, r, b = box
    return ((r - l) * 1920 / 1024, (b - t) * 1080 / 576)


def make_logo_transparent(logo: Image.Image) -> Image.Image:
    rgba = logo.convert("RGBA")
    px = rgba.load()
    w, h = rgba.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if r > 160 and g > 125 and b > 90 and abs(r - g) < 60:
                px[x, y] = (r, g, b, 0)
                continue
            lum = 0.299 * r + 0.587 * g + 0.114 * b
            if lum > 205 and max(r, g, b) - min(r, g, b) < 45:
                px[x, y] = (r, g, b, 0)
    return rgba


def inpaint_ui_areas(img_rgb: np.ndarray, masks: list[tuple[int, int, int, int]]) -> np.ndarray:
    h, w = img_rgb.shape[:2]
    mask = np.zeros((h, w), dtype=np.uint8)
    for box in masks:
        l, t, r, b = box
        pad = 6
        mask[max(0, t - pad):min(h, b + pad), max(0, l - pad):min(w, r + pad)] = 255
    kernel = np.ones((7, 7), np.uint8)
    mask = cv2.dilate(mask, kernel, iterations=2)
    return cv2.inpaint(img_rgb, mask, inpaintRadius=14, flags=cv2.INPAINT_NS)


def draw_menu_button(
    label: str,
    size: tuple[int, int],
    fill: tuple[int, int, int, int],
    text_fill: tuple[int, int, int, int],
) -> Image.Image:
    w, h = size
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    slant = int(h * 0.22)
    poly = [(0, 0), (w - slant, 0), (w, h), (slant, h)]
    draw.polygon(poly, fill=fill)

    try:
        font = ImageFont.truetype("arialbd.ttf", max(18, int(h * 0.42)))
    except OSError:
        font = ImageFont.load_default()

    bbox = draw.textbbox((0, 0), label, font=font)
    tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
    draw.text(((w - tw) * 0.5, (h - th) * 0.5 - 2), label, font=font, fill=text_fill)
    return img


def write_anchors_file(anchors: list[tuple[float, float]], size: tuple[float, float]) -> None:
    path = MENU / "menu_layout.txt"
    lines = [
        "# Gerado por Tools/slice_menu_art.py — referência 1920×1080",
        f"buttonSize={size[0]:.1f},{size[1]:.1f}",
    ]
    names = ["JOGAR", "OPCOES", "CREDITOS", "SAIR"]
    for name, (x, y) in zip(names, anchors):
        lines.append(f"{name}={x:.4f},{y:.4f}")
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> None:
    if not SOURCE.exists():
        raise SystemExit(f"Arquivo não encontrado: {SOURCE}")

    im = Image.open(SOURCE).convert("RGB")
    w, h = im.size
    PREVIEW.mkdir(parents=True, exist_ok=True)

    boxes = {k: scale_box(v, w, h) for k, v in REGIONS_1024.items()}
    anchors = [center_anchor(REGIONS_1024[k], 1024, 576) for k in (
        "btn_jogar", "btn_opcoes", "btn_creditos", "btn_sair"
    )]
    btn_size = button_size_ref(REGIONS_1024["btn_jogar"])
    write_anchors_file(anchors, btn_size)

    logo_crop = im.crop(boxes["logo"])
    make_logo_transparent(logo_crop).save(MENU / "menu_logo.png")

    arr = np.array(im)
    clean = inpaint_ui_areas(arr, [
        boxes["logo"],
        boxes["btn_jogar"],
        boxes["btn_opcoes"],
        boxes["btn_creditos"],
        boxes["btn_sair"],
    ])
    Image.fromarray(clean).save(MENU / "menu_background_clean.png")

    # Sprites vetoriais (melhor que recorte da cena composta).
    bw, bh = boxes["btn_jogar"][2] - boxes["btn_jogar"][0], boxes["btn_jogar"][3] - boxes["btn_jogar"][1]
    draw_menu_button("JOGAR", (bw, bh), (242, 199, 38, 255), (20, 20, 20, 255)).save(
        MENU / "menu_button_highlight.png"
    )
    draw_menu_button("OPÇÕES", (bw, bh), (24, 24, 24, 235), (245, 245, 245, 255)).save(
        MENU / "menu_button_normal.png"
    )

    preview = im.copy()
    draw = ImageDraw.Draw(preview)
    for name, box in boxes.items():
        l, t, r, b = box
        draw.rectangle((l, t, r, b), outline=(255, 0, 255), width=2)
    preview.save(PREVIEW / "regions_boxed.png")
    Image.fromarray(clean).save(PREVIEW / "menu_background_clean_preview.png")
    make_logo_transparent(logo_crop).save(PREVIEW / "menu_logo_preview.png")

    print("Arte gerada em", MENU)
    print("Anchors (0-1):", anchors)
    print("Button size ref:", btn_size)


if __name__ == "__main__":
    main()
