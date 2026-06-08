"""Recorta cada botão da folha com caixas precisas e remove fundo preto."""
from __future__ import annotations

from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
BUTTONS = ROOT / "Assets" / "UI" / "Menu" / "Buttons"
SHEET = ROOT / "Assets" / "UI" / "Menu" / "menu_buttons_sheet.png"
JOGAR_SELECTED_SOURCE = ROOT / "Assets" / "UI" / "Menu" / "btn_jogar_recomeco.png"
CREDITOS_NORMAL_SOURCE = ROOT / "Assets" / "UI" / "Menu" / "btn_creditos_recomeco.png"
SAIR_NORMAL_SOURCE = ROOT / "Assets" / "UI" / "Menu" / "btn_sair_recomeco.png"
SAIR_HOVER_SOURCE = ROOT / "Assets" / "UI" / "Menu" / "btn_sair_selected_recomeco.png"

# Caixas detectadas no PNG 1024×682 (um botão por arquivo).
CONTENT_SLICES: dict[str, tuple[int, int, int, int]] = {
    "opcoes_hover.png": (76, 242, 483, 317),
    "creditos_hover.png": (538, 240, 954, 317),
    "jogar_normal.png": (67, 363, 487, 472),
    "opcoes_normal.png": (533, 363, 955, 472),
}

CUSTOM_SOURCES: dict[str, Path] = {
    "creditos_normal.png": CREDITOS_NORMAL_SOURCE,
    "sair_normal.png": SAIR_NORMAL_SOURCE,
    "sair_hover.png": SAIR_HOVER_SOURCE,
}


def black_to_alpha(img: Image.Image, threshold: int = 24) -> Image.Image:
    rgba = img.convert("RGBA")
    px = np.array(rgba)
    rgb = px[:, :, :3]
    dark = np.max(rgb, axis=2) < threshold
    px[dark, 3] = 0
    return Image.fromarray(px)


def load_custom_button(path: Path) -> Image.Image:
    img = Image.open(path).convert("RGBA")
    px = np.array(img)
    if np.all(px[:, :, 3] >= 250):
        r, g, b = px[:, :, 0], px[:, :, 1], px[:, :, 2]
        yellow = (r > 150) & (g > 100) & (b < 120)
        black = (r < 60) & (g < 60) & (b < 60)
        keep = yellow | black
        ys, xs = np.where(keep)
        if len(xs) > 0:
            l, r = max(0, xs.min() - 2), min(px.shape[1], xs.max() + 3)
            t, b = max(0, ys.min() - 2), min(px.shape[0], ys.max() + 3)
            px = px[t:b, l:r]
            cr, cg, cb = px[:, :, 0], px[:, :, 1], px[:, :, 2]
            keep = (
                (cr > 150) & (cg > 100) & (cb < 120)
            ) | ((cr < 60) & (cg < 60) & (cb < 60))
            px[~keep, 3] = 0
            return trim_transparent(Image.fromarray(px))

    img = black_to_alpha(img, threshold=22)
    px = np.array(img)
    rgb = px[:, :, :3]
    gray = (
        (np.abs(rgb[:, :, 0].astype(int) - rgb[:, :, 1]) < 8)
        & (np.abs(rgb[:, :, 1].astype(int) - rgb[:, :, 2]) < 8)
        & (rgb[:, :, 0] > 90)
        & (rgb[:, :, 0] < 210)
    )
    px[gray, 3] = 0
    return trim_transparent(Image.fromarray(px))


def trim_transparent(img: Image.Image, padding: int = 1) -> Image.Image:
    rgba = img.convert("RGBA")
    arr = np.array(rgba)
    alpha = arr[:, :, 3]
    ys, xs = np.where(alpha > 8)
    if len(xs) == 0:
        return img
    l, r = max(0, xs.min() - padding), min(arr.shape[1], xs.max() + padding + 1)
    t, b = max(0, ys.min() - padding), min(arr.shape[0], ys.max() + padding + 1)
    return rgba.crop((l, t, r, b))


def make_jogar_selected_from_normal(
    normal: Image.Image, compact_yellow: Image.Image
) -> Image.Image:
    """Versão amarela do JOGAR (mesmo tamanho do normal, cores da folha)."""
    px = np.array(normal.convert("RGBA"))
    ref = np.array(black_to_alpha(compact_yellow.convert("RGBA")))
    visible = px[:, :, 3] > 20

    yellow_mask = (ref[:, :, 0] > 190) & (ref[:, :, 1] > 140) & (ref[:, :, 2] < 140)
    if yellow_mask.any():
        yellow = ref[yellow_mask, :3].mean(axis=0).astype(np.uint8)
    else:
        yellow = np.array([255, 198, 42], dtype=np.uint8)

    r = px[:, :, 0].astype(np.float32)
    g = px[:, :, 1].astype(np.float32)
    b = px[:, :, 2].astype(np.float32)
    lum = 0.299 * r + 0.587 * g + 0.114 * b

    is_ink = visible & (
        (lum > 168)
        | ((r > 135) & (g > 105) & (b < 135))
    )

    out = np.zeros_like(px)
    out[visible, 0] = yellow[0]
    out[visible, 1] = yellow[1]
    out[visible, 2] = yellow[2]
    out[visible, 3] = px[visible, 3]

    out[is_ink, 0] = 10
    out[is_ink, 1] = 10
    out[is_ink, 2] = 10

    ys = np.where(visible)[0]
    if len(ys) > 0:
        top, bottom = ys.min(), ys.max()
        bar = 3
        for y in range(top, min(top + bar, out.shape[0])):
            row = visible[y]
            out[y, row] = (10, 10, 10, 255)
        for y in range(max(top, bottom - bar + 1), bottom + 1):
            row = visible[y]
            out[y, row] = (10, 10, 10, 255)

    return Image.fromarray(out.astype(np.uint8))


def add_gold_border(base: Image.Image, thickness: int = 3) -> Image.Image:
    """Moldura dourada simples para o hover do SAIR."""
    img = base.convert("RGBA").copy()
    draw = ImageDraw.Draw(img)
    w, h = img.size
    gold = (214, 176, 44, 255)
    for i in range(thickness):
        draw.rectangle((i, i, w - 1 - i, h - 1 - i), outline=gold)
    return img


def main() -> None:
    if not SHEET.exists():
        raise SystemExit(f"Folha não encontrada: {SHEET}")

    BUTTONS.mkdir(parents=True, exist_ok=True)
    sheet = Image.open(SHEET).convert("RGBA")

    processed: dict[str, Image.Image] = {}
    for name, box in CONTENT_SLICES.items():
        crop = sheet.crop(box)
        crop = black_to_alpha(crop)
        crop = trim_transparent(crop)
        crop.save(BUTTONS / name)
        processed[name] = crop

    if JOGAR_SELECTED_SOURCE.exists():
        jogar_selected = black_to_alpha(Image.open(JOGAR_SELECTED_SOURCE).convert("RGBA"))
        jogar_selected = trim_transparent(jogar_selected)
    else:
        compact_yellow = sheet.crop((75, 123, 485, 170))
        jogar_selected = make_jogar_selected_from_normal(
            processed["jogar_normal.png"], compact_yellow
        )
    jogar_selected.save(BUTTONS / "jogar_selected.png")
    processed["jogar_selected.png"] = jogar_selected

    for name, source in CUSTOM_SOURCES.items():
        if not source.exists():
            continue
        custom = load_custom_button(source)
        custom.save(BUTTONS / name)
        processed[name] = custom

    if "sair_hover.png" not in processed and "sair_normal.png" in processed:
        sair_hover = add_gold_border(processed["sair_normal.png"])
        sair_hover.save(BUTTONS / "sair_hover.png")
        processed["sair_hover.png"] = sair_hover

    legacy = BUTTONS / "jogar_hover.png"
    if legacy.exists():
        legacy.unlink()
    legacy_meta = BUTTONS / "jogar_hover.png.meta"
    if legacy_meta.exists():
        legacy_meta.unlink()

    print("Sprites recortados em", BUTTONS)
    for path in sorted(BUTTONS.glob("*.png")):
        im = Image.open(path)
        print(f"  {path.name}: {im.size}")

    update_button_set_asset()


def read_guid(png_path: Path) -> str:
    meta = png_path.with_suffix(png_path.suffix + ".meta")
    if not meta.exists():
        raise FileNotFoundError(f"Meta não encontrado: {meta}")
    for line in meta.read_text(encoding="utf-8").splitlines():
        if line.startswith("guid: "):
            return line.split("guid: ", 1)[1].strip()
    raise ValueError(f"GUID ausente em {meta}")


def sprite_ref(guid: str) -> str:
    return f"{{fileID: 21300000, guid: {guid}, type: 3}}"


def update_button_set_asset() -> None:
    asset_path = ROOT / "Assets" / "Resources" / "MainMenuButtonSet.asset"
    script_guid = "48c9c31dd8c83bc4f8a600e20e93dd51"

    entries = {
        "jogar": ("jogar_normal.png", "jogar_selected.png", "jogar_selected.png"),
        "opcoes": ("opcoes_normal.png", "opcoes_hover.png", None),
        "creditos": ("creditos_normal.png", "creditos_hover.png", None),
        "sair": ("sair_normal.png", "sair_hover.png", None),
    }

    lines = [
        "%YAML 1.1",
        "%TAG !u! tag:unity3d.com,2011:",
        "--- !u!114 &11400000",
        "MonoBehaviour:",
        "  m_ObjectHideFlags: 0",
        "  m_CorrespondingSourceObject: {fileID: 0}",
        "  m_PrefabInstance: {fileID: 0}",
        "  m_PrefabAsset: {fileID: 0}",
        "  m_GameObject: {fileID: 0}",
        "  m_Enabled: 1",
        "  m_EditorHideFlags: 0",
        f"  m_Script: {{fileID: 11500000, guid: {script_guid}, type: 3}}",
        "  m_Name: MainMenuButtonSet",
        "  m_EditorClassIdentifier: ",
    ]

    for entry_name, (normal, hover, selected) in entries.items():
        lines.append(f"  {entry_name}:")
        lines.append(f"    normal: {sprite_ref(read_guid(BUTTONS / normal))}")
        lines.append(f"    hover: {sprite_ref(read_guid(BUTTONS / hover))}")
        if selected:
            lines.append(f"    selected: {sprite_ref(read_guid(BUTTONS / selected))}")
        else:
            lines.append("    selected: {fileID: 0}")

    asset_path.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print("MainMenuButtonSet.asset atualizado")


if __name__ == "__main__":
    main()
