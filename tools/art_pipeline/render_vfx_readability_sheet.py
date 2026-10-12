from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageOps


ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "docs/evidence/visual-development/vfx-readability-contact-sheet-2026-08-17.png"


SPECS = [
    ("Knight Slash", "diamond", (255, 194, 46), "PhysicalLight"),
    ("Raider Hit", "diamond", (255, 71, 31), "PhysicalLight"),
    ("Boss Attack", "diamond", (255, 77, 20), "PhysicalHeavy High"),
    ("Boss Phase", "ring", (255, 41, 20), "Status Critical"),
    ("Archer Arrow", "diamond", (255, 173, 46), "Projectile"),
    ("Mage Arcane", "diamond", (61, 191, 240), "Magical"),
    ("Support Heal", "ring", (87, 255, 122), "Heal"),
    ("Regen Pulse", "ring", (46, 235, 87), "Regen Enemy"),
    ("Slow Marker", "ring", (61, 191, 240), "Status Slow"),
    ("Disrupt", "ring", (217, 31, 41), "Status Dark"),
]


def font(size):
    for candidate in (
        "/System/Library/Fonts/Supplemental/Arial.ttf",
        "/System/Library/Fonts/Helvetica.ttc",
    ):
        try:
            return ImageFont.truetype(candidate, size)
        except OSError:
            pass
    return ImageFont.load_default()


TITLE = font(30)
LABEL = font(18)
SMALL = font(14)


def luminance(color):
    r, g, b = color
    return int(0.2126 * r + 0.7152 * g + 0.0722 * b)


def draw_shape(draw, center, shape, color, scale=1.0):
    cx, cy = center
    if shape == "diamond":
        w = int(44 * scale)
        h = int(62 * scale)
        points = [(cx, cy - h), (cx + w, cy), (cx, cy + h), (cx - w, cy)]
        draw.polygon(points, fill=color)
        draw.line(points + [points[0]], fill=(255, 255, 255), width=2)
        return

    radius = int(52 * scale)
    width = max(8, int(13 * scale))
    box = [cx - radius, cy - radius, cx + radius, cy + radius]
    draw.ellipse(box, outline=color, width=width)
    draw.ellipse([cx - 8, cy - 8, cx + 8, cy + 8], fill=color)


def draw_cell(base, x, y, spec, grayscale=False):
    name, shape, color, tag = spec
    panel = Image.new("RGB", (210, 180), (18, 24, 30))
    d = ImageDraw.Draw(panel)
    draw_color = (luminance(color),) * 3 if grayscale else color
    draw_shape(d, (105, 82), shape, draw_color)
    d.text((14, 132), name, fill=(238, 242, 245), font=LABEL)
    d.text((14, 154), tag, fill=(174, 187, 195), font=SMALL)
    base.paste(panel, (x, y))


def draw_dense_panel(base, x, y, grayscale=False):
    panel = Image.new("RGB", (450, 392), (15, 20, 25))
    d = ImageDraw.Draw(panel)
    normal = SPECS[:9]
    for index, spec in enumerate(normal * 2):
        _, shape, color, _ = spec
        px = 48 + (index % 6) * 64
        py = 76 + (index // 6) * 76
        draw_color = (luminance(color),) * 3 if grayscale else color
        draw_shape(d, (px, py), shape, draw_color, 0.55)

    boss_phase = SPECS[3]
    boss_color = (luminance(boss_phase[2]),) * 3 if grayscale else boss_phase[2]
    draw_shape(d, (330, 240), boss_phase[1], boss_color, 1.55)
    d.text((18, 18), "Dense combat + boss telegraph", fill=(244, 247, 250), font=LABEL)
    d.text((18, 44), "Critical ring must remain readable", fill=(178, 190, 200), font=SMALL)
    d.rectangle([286, 194, 374, 286], outline=(255, 255, 255), width=2)
    base.paste(panel, (x, y))


def main():
    OUT.parent.mkdir(parents=True, exist_ok=True)
    width, height = 960, 1640
    image = Image.new("RGB", (width, height), (10, 14, 18))
    draw = ImageDraw.Draw(image)
    draw.text((32, 28), "ALLIN: Defense VFX Readability QA", fill=(248, 250, 252), font=TITLE)
    draw.text((32, 66), "Color and grayscale review for VIS-514", fill=(178, 190, 200), font=LABEL)
    draw.text((32, 112), "Color", fill=(238, 242, 245), font=LABEL)
    draw.text((272, 112), "Grayscale", fill=(238, 242, 245), font=LABEL)
    draw.text((522, 112), "Dense Stack", fill=(238, 242, 245), font=LABEL)

    y = 150
    for index, spec in enumerate(SPECS):
        row = index % 5
        col_group = index // 5
        x_offset = 32 + col_group * 480
        y_pos = y + row * 202
        draw_cell(image, x_offset, y_pos, spec, False)
        draw_cell(image, x_offset + 230, y_pos, spec, True)

    draw_dense_panel(image, 32, 1190, False)
    draw_dense_panel(image, 510, 1190, True)
    image = ImageOps.expand(image, border=2, fill=(68, 78, 88))
    image.save(OUT)
    print(OUT)


if __name__ == "__main__":
    main()
