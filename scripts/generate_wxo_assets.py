"""Generate WXO brand assets (app icons, splash, favicon) from the master logo.

Usage:
    python scripts/generate_wxo_assets.py <source_logo.png>

Writes transparent-background PNGs into MatchIQ_App/assets and
MatchIQ_Flutter/assets/images.
"""

import sys
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent.parent
RN_ASSETS = ROOT / "MatchIQ_App" / "assets"
FLUTTER_ASSETS = ROOT / "MatchIQ_Flutter" / "assets" / "images"

BRAND_BG = (17, 17, 17, 255)


def load_master(path: Path) -> Image.Image:
    img = Image.open(path).convert("RGBA")
    size = max(img.size)
    square = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    square.paste(img, ((size - img.width) // 2, (size - img.height) // 2))
    return square


def circular_cutout(img: Image.Image) -> Image.Image:
    """The badge is a circle on black — mask the square corners away."""
    size = img.size[0]
    mask = Image.new("L", (size, size), 0)
    ImageDraw.Draw(mask).ellipse((0, 0, size - 1, size - 1), fill=255)
    out = img.copy()
    out.putalpha(mask)
    return out


def save(img: Image.Image, path: Path, size: int, background: tuple | None = None) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    resized = img.resize((size, size), Image.LANCZOS)
    if background:
        canvas = Image.new("RGBA", (size, size), background)
        canvas.alpha_composite(resized)
        resized = canvas
    resized.save(path, "PNG", optimize=True)
    print(f"  {path.relative_to(ROOT)}  {size}x{size}")


def padded(img: Image.Image, size: int, scale: float) -> Image.Image:
    """Logo centred inside a transparent canvas (adaptive icon safe zone)."""
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    inner = int(size * scale)
    canvas.alpha_composite(img.resize((inner, inner), Image.LANCZOS), ((size - inner) // 2,) * 2)
    return canvas


def main() -> None:
    if len(sys.argv) < 2:
        raise SystemExit("usage: generate_wxo_assets.py <source_logo.png>")

    master = circular_cutout(load_master(Path(sys.argv[1])))

    print("React Native (MatchIQ_App/assets):")
    save(master, RN_ASSETS / "wxo-logo.png", 1024)
    save(master, RN_ASSETS / "icon.png", 1024, background=BRAND_BG)
    save(master, RN_ASSETS / "splash-icon.png", 1024)
    save(master, RN_ASSETS / "favicon.png", 96, background=BRAND_BG)
    save(padded(master, 1024, 0.66), RN_ASSETS / "android-icon-foreground.png", 1024)
    save(padded(master, 1024, 0.66), RN_ASSETS / "android-icon-monochrome.png", 1024)

    background = Image.new("RGBA", (1024, 1024), BRAND_BG)
    background.save(RN_ASSETS / "android-icon-background.png", "PNG", optimize=True)
    print("  MatchIQ_App/assets/android-icon-background.png  1024x1024")

    print("Flutter (MatchIQ_Flutter/assets/images):")
    save(master, FLUTTER_ASSETS / "wxo_logo.png", 1024)


if __name__ == "__main__":
    main()
