"""Swap the Mahjong game background sprite while keeping the Unity GUID intact.

The scene camera is orthographic at size 9.6 (19.2 units tall) and the sprite is
imported at 100 pixels-per-unit, so the art must be 1440x1920 to still cover the
widest supported aspect (0.75). Source art authored for 9:16 is centred at its
native size and the side margins are filled with a mirrored, blurred, darkened
extension so tablets never see letterbox bars.

Usage:
    python scripts/replace_mahjong_background.py <source.png>
"""

import shutil
import sys
from pathlib import Path

from PIL import Image, ImageEnhance, ImageFilter

ROOT = Path(__file__).resolve().parent.parent
BG_DIR = ROOT / "Unity_Game" / "Assets" / "Mahjong" / "Sprites" / "Game Backgrounds"
TARGET = BG_DIR / "Bkg Green.png"
BACKUP = BG_DIR / "Bkg Green.previous.png"

CANVAS = (1440, 1920)
EDGE_SAMPLE = 120


def side_fill(art: Image.Image, width: int, height: int, *, left: bool) -> Image.Image:
    """Mirror the art's edge strip outwards, blurred and dimmed."""
    strip = art.crop((0, 0, EDGE_SAMPLE, art.height)) if left else art.crop(
        (art.width - EDGE_SAMPLE, 0, art.width, art.height)
    )
    strip = strip.transpose(Image.FLIP_LEFT_RIGHT).resize((width, height), Image.LANCZOS)
    strip = strip.filter(ImageFilter.GaussianBlur(radius=28))
    return ImageEnhance.Brightness(strip).enhance(0.55)


def build(source: Path) -> Image.Image:
    art = Image.open(source).convert("RGBA")

    # Scale to the canvas height, preserving the authored composition.
    scale = CANVAS[1] / art.height
    art = art.resize((round(art.width * scale), CANVAS[1]), Image.LANCZOS)
    if art.width > CANVAS[0]:
        offset = (art.width - CANVAS[0]) // 2
        art = art.crop((offset, 0, offset + CANVAS[0], CANVAS[1]))

    canvas = Image.new("RGBA", CANVAS, (0, 0, 0, 255))
    margin = (CANVAS[0] - art.width) // 2
    if margin > 0:
        canvas.paste(side_fill(art, margin + 2, CANVAS[1], left=True), (0, 0))
        canvas.paste(
            side_fill(art, margin + 2, CANVAS[1], left=False), (CANVAS[0] - margin - 2, 0)
        )
    canvas.alpha_composite(art, (margin, 0))
    return canvas


def main() -> None:
    if len(sys.argv) < 2:
        raise SystemExit("usage: replace_mahjong_background.py <source.png>")
    source = Path(sys.argv[1])
    if not source.exists():
        raise SystemExit(f"source not found: {source}")

    shutil.copy2(TARGET, BACKUP)
    print(f"backed up -> {BACKUP.relative_to(ROOT)}")

    build(source).save(TARGET, "PNG", optimize=True)
    out = Image.open(TARGET)
    print(f"written   -> {TARGET.relative_to(ROOT)}  {out.size[0]}x{out.size[1]}")


if __name__ == "__main__":
    main()
