"""Refresh the prebuilt Android res/ launcher + splash images from the WXO logo.

Expo only regenerates these during `prebuild`, so update them in place to keep
release builds in sync with assets/.

Usage:
    python scripts/generate_android_icons.py <source_logo.png>
"""

import sys
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent.parent
RES = ROOT / "MatchIQ_App" / "android" / "app" / "src" / "main" / "res"

BRAND_BG = (17, 17, 17, 255)
DENSITIES = {"mdpi": 1, "hdpi": 1.5, "xhdpi": 2, "xxhdpi": 3, "xxxhdpi": 4}

LAUNCHER_DP = 48
ADAPTIVE_DP = 108
SPLASH_DP = 200


def master(path: Path) -> Image.Image:
    img = Image.open(path).convert("RGBA")
    side = max(img.size)
    square = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    square.paste(img, ((side - img.width) // 2, (side - img.height) // 2))
    mask = Image.new("L", (side, side), 0)
    ImageDraw.Draw(mask).ellipse((0, 0, side - 1, side - 1), fill=255)
    square.putalpha(mask)
    return square


def scaled(img: Image.Image, px: int, *, inset: float = 1.0, bg=None) -> Image.Image:
    canvas = Image.new("RGBA", (px, px), bg or (0, 0, 0, 0))
    inner = max(1, int(px * inset))
    canvas.alpha_composite(img.resize((inner, inner), Image.LANCZOS), ((px - inner) // 2,) * 2)
    return canvas


def write(img: Image.Image, path: Path) -> None:
    if not path.parent.exists():
        return
    fmt = "WEBP" if path.suffix == ".webp" else "PNG"
    img.save(path, fmt, lossless=True) if fmt == "WEBP" else img.save(path, fmt, optimize=True)
    print(f"  {path.relative_to(ROOT)}  {img.width}x{img.height}")


def main() -> None:
    if len(sys.argv) < 2:
        raise SystemExit("usage: generate_android_icons.py <source_logo.png>")
    logo = master(Path(sys.argv[1]))

    for density, factor in DENSITIES.items():
        mip = RES / f"mipmap-{density}"
        launcher = int(LAUNCHER_DP * factor)
        adaptive = int(ADAPTIVE_DP * factor)

        write(scaled(logo, launcher, bg=BRAND_BG), mip / "ic_launcher.webp")
        write(scaled(logo, launcher, bg=BRAND_BG), mip / "ic_launcher_round.webp")
        write(scaled(logo, adaptive, inset=0.66), mip / "ic_launcher_foreground.webp")
        write(scaled(logo, adaptive, inset=0.66), mip / "ic_launcher_monochrome.webp")
        write(
            Image.new("RGBA", (adaptive, adaptive), BRAND_BG),
            mip / "ic_launcher_background.webp",
        )

        write(
            scaled(logo, int(SPLASH_DP * factor)),
            RES / f"drawable-{density}" / "splashscreen_logo.png",
        )


if __name__ == "__main__":
    main()
