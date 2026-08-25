"""Compose a 64 px actor icon readability review sheet from rendered role icons."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROLES = (
    "Knight",
    "Guard",
    "Raider",
    "Vanguard",
    "Regenerator",
    "Support",
    "Siege",
    "Boss",
)

LABELS = {
    "Regenerator": "Regen",
}


def load_font(size: int) -> ImageFont.ImageFont:
    for path in (
        "/System/Library/Fonts/Supplemental/Arial.ttf",
        "/System/Library/Fonts/Helvetica.ttc",
    ):
        candidate = Path(path)
        if candidate.exists():
            return ImageFont.truetype(str(candidate), size)
    return ImageFont.load_default()


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--icons-dir", default="docs/evidence/visual-development/actor-icons-2026-08-17")
    parser.add_argument("--output", default="docs/evidence/visual-development/actor-icon-64px-review-2026-08-17.png")
    args = parser.parse_args()

    icons_dir = Path(args.icons_dir)
    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)

    icon_size = 64
    cell_width = 92
    label_height = 24
    row_gap = 24
    margin = 18
    title_height = 34
    width = margin * 2 + cell_width * len(ROLES)
    height = margin + title_height + icon_size * 2 + label_height + row_gap + margin

    sheet = Image.new("RGBA", (width, height), (17, 22, 30, 255))
    draw = ImageDraw.Draw(sheet)
    title_font = load_font(17)
    label_font = load_font(11)

    draw.text((margin, margin), "Actor icon 64 px review", fill=(232, 238, 244, 255), font=title_font)
    color_y = margin + title_height
    grayscale_y = color_y + icon_size + row_gap
    draw.text((margin, color_y - 16), "Color", fill=(156, 170, 184, 255), font=label_font)
    draw.text((margin, grayscale_y - 16), "Grayscale", fill=(156, 170, 184, 255), font=label_font)

    for index, role in enumerate(ROLES):
        x = margin + index * cell_width
        icon = Image.open(icons_dir / f"{role}.png").convert("RGBA")
        icon.thumbnail((icon_size, icon_size), Image.Resampling.LANCZOS)
        grayscale = Image.merge(
            "RGBA",
            (
                icon.convert("L"),
                icon.convert("L"),
                icon.convert("L"),
                icon.getchannel("A"),
            ),
        )
        for row_y, row_icon in ((color_y, icon), (grayscale_y, grayscale)):
            icon_x = x + (cell_width - row_icon.width) // 2
            icon_y = row_y + (icon_size - row_icon.height) // 2
            draw.rounded_rectangle(
                (x + 8, row_y - 4, x + cell_width - 8, row_y + icon_size + 4),
                radius=6,
                fill=(31, 38, 48, 255),
                outline=(76, 88, 104, 255),
            )
            sheet.alpha_composite(row_icon, (icon_x, icon_y))
        label = LABELS.get(role, role if len(role) <= 10 else role[:9] + ".")
        bbox = draw.textbbox((0, 0), label, font=label_font)
        draw.text(
            (x + (cell_width - (bbox[2] - bbox[0])) / 2, grayscale_y + icon_size + 8),
            label,
            fill=(210, 218, 226, 255),
            font=label_font,
        )

    sheet.save(output)
    print(f"[DC_ACTOR_ICON_REVIEW] {output}")


if __name__ == "__main__":
    main()
