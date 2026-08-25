#!/usr/bin/env python3
"""Render VIS-409 dressing placement manifest as a top-down plan sheet.

Usage:
  python3 tools/art_pipeline/render_environment_dressing_layout.py -- \
    --layout docs/evidence/visual-development/environment-dressing-layout-2026-08-16.json \
    --paintover-data docs/evidence/visual-development/greenwatch-valley-paintover-plan-2026-08-16.json \
    --output docs/evidence/visual-development/environment-dressing-layout-2026-08-16.png
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any

from PIL import Image, ImageDraw, ImageFont


WIDTH = 1920
HEIGHT = 1080
MAP_LEFT = 118
MAP_TOP = 132
MAP_RIGHT = 1802
MAP_BOTTOM = 824
DEFAULT_MIN_X = -3.2
DEFAULT_MAX_X = 25.4
DEFAULT_MIN_Z = -2.1
DEFAULT_MAX_Z = 13.2


FAMILY_STYLE = {
    "Tree": {
        "fill": (120, 195, 255, 220),
        "outline": (198, 228, 255, 245),
        "radius": 14,
        "label": "Tree",
    },
    "Rock": {
        "fill": (151, 151, 151, 220),
        "outline": (228, 228, 228, 238),
        "radius": 12,
        "label": "Rock",
    },
    "Bush": {
        "fill": (111, 191, 111, 210),
        "outline": (201, 240, 201, 232),
        "radius": 10,
        "label": "Bush",
    },
    "Grass": {
        "fill": (122, 194, 110, 190),
        "outline": (216, 243, 211, 220),
        "radius": 8,
        "label": "Grass",
    },
    "Crystal": {
        "fill": (95, 228, 255, 210),
        "outline": (200, 255, 255, 245),
        "radius": 11,
        "label": "Crystal",
    },
}


def load_font(size: int) -> ImageFont.ImageFont:
    candidates = [
        "/System/Library/Fonts/Supplemental/Arial.ttf",
        "/System/Library/Fonts/Arial.ttf",
    ]
    for path in candidates:
        if Path(path).exists():
            try:
                return ImageFont.truetype(path, size)
            except OSError:
                continue
    return ImageFont.load_default()


FONT_TITLE = load_font(34)
FONT_H2 = load_font(22)
FONT_BODY = load_font(18)
FONT_SMALL = load_font(14)


def world_to_px(x: float, z: float, bounds: dict[str, float]) -> tuple[int, int]:
    px = MAP_LEFT + (x - bounds["min_x"]) / (bounds["max_x"] - bounds["min_x"]) * (MAP_RIGHT - MAP_LEFT)
    py = MAP_BOTTOM - (z - bounds["min_z"]) / (bounds["max_z"] - bounds["min_z"]) * (MAP_BOTTOM - MAP_TOP)
    return round(px), round(py)


def draw_label(draw: ImageDraw.ImageDraw, xy: tuple[int, int], text: str, font: ImageFont.ImageFont,
               fill=(226, 238, 255), anchor="mm", bg=None) -> None:
    if bg is not None:
        box = draw.textbbox(xy, text, font=font, anchor=anchor)
        pad = 5
        draw.rounded_rectangle((box[0] - pad, box[1] - pad, box[2] + pad, box[3] + pad), 7, fill=bg)
    draw.text(xy, text, font=font, fill=fill, anchor=anchor)


def draw_polyline(draw: ImageDraw.ImageDraw, points: list[tuple[int, int]], fill: tuple[int, int, int], width: int,
                 joint="curve") -> None:
    draw.line(points, fill=fill, width=width, joint=joint)
    radius = width // 2
    for x, y in points:
        draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=fill)


def draw_slot(draw: ImageDraw.ImageDraw, code: str, x: float, z: float, role: str, bounds: dict[str, float]) -> None:
    px, py = world_to_px(x, z, bounds)
    color = {
        "Frontline": (255, 191, 92),
        "Ranged": (118, 184, 255),
        "Control": (123, 231, 193),
    }.get(role, (198, 220, 255))
    draw.ellipse((px - 30, py - 30, px + 30, py + 30), outline=color, width=5, fill=(12, 24, 38, 190))
    draw.ellipse((px - 50, py - 50, px + 50, py + 50), outline=color + (110,), width=2)
    draw_label(draw, (px, py - 2), code, FONT_BODY, fill=(255, 255, 255), bg=(8, 15, 25, 180))
    draw_label(draw, (px, py + 47), role[0], FONT_SMALL, fill=color, bg=(8, 15, 25, 165))


def draw_placeholders(draw: ImageDraw.ImageDraw, layout: dict[str, Any], paintover: dict[str, Any]) -> None:
    bounds = layout.get("world_bounds", {
        "min_x": DEFAULT_MIN_X,
        "max_x": DEFAULT_MAX_X,
        "min_z": DEFAULT_MIN_Z,
        "max_z": DEFAULT_MAX_Z,
    })

    # Base map
    draw.rounded_rectangle((MAP_LEFT - 20, MAP_TOP - 26, MAP_RIGHT + 20, MAP_BOTTOM + 26), 26,
                           fill=(12, 26, 34), outline=(76, 114, 143), width=2)
    draw.rectangle((MAP_LEFT, MAP_TOP, MAP_RIGHT, MAP_BOTTOM), fill=(17, 64, 50))

    # Keep slot and route context from paintover plan
    if paintover.get("regions"):
        for region in paintover["regions"]:
            points = [world_to_px(point["x"], point["z"], bounds) for point in region.get("polygon", [])]
            if len(points) >= 3:
                draw.polygon(points, fill=(80, 110, 145, 34), outline=(150, 180, 220), width=2)
    if paintover.get("noise_bands"):
        for band in paintover["noise_bands"]:
            p1 = world_to_px(band["min_x"], band["min_z"], bounds)
            p2 = world_to_px(band["max_x"], band["max_z"], bounds)
            draw.rounded_rectangle((min(p1[0], p2[0]), min(p1[1], p2[1]), max(p1[0], p2[0]), max(p1[1], p2[1])),
                                   11, outline=(111, 154, 200), width=2, fill=(20, 34, 50, 55))

    # Path read layer
    path = [(entry["x"], entry["z"]) for entry in layout["path"]]
    path_px = [world_to_px(x, z, bounds) for x, z in path]
    draw_polyline(draw, path_px, (86, 96, 103), 84)
    draw_polyline(draw, path_px, (128, 91, 57), 58)
    draw_polyline(draw, path_px, (165, 122, 82), 12)

    # Slot rings + no-clutter buffers
    for slot in layout["slots"]:
        slot_radius = (layout["slot_clear_radius"] + layout["slot_hit_margin"] + 0.4) * 28
        px, py = world_to_px(slot["x"], slot["z"], bounds)
        draw.ellipse((px - slot_radius, py - slot_radius, px + slot_radius, py + slot_radius),
                     outline=(255, 232, 128, 150), width=2)
        draw_slot(draw, slot["code"], slot["x"], slot["z"], slot["role"], bounds)

    # Path no-clutter bands
    for i in range(len(path) - 1):
        a = world_to_px(path[i][0], path[i][1], bounds)
        b = world_to_px(path[i + 1][0], path[i + 1][1], bounds)
        pxa, pya = a
        pxb, pyb = b
        # draw a simple buffered line using parallel approximation
        draw.line((pxa, pya, pxb, pyb), fill=(170, 190, 210, 52), width=58)

    # Placements
    for item in layout["placements"]:
        style = FAMILY_STYLE.get(item["family"], FAMILY_STYLE["Grass"])
        px, py = world_to_px(item["x"], item["z"], bounds)
        r = style["radius"]
        draw.ellipse((px - r, py - r, px + r, py + r),
                     fill=style["fill"], outline=style["outline"], width=2)
        draw_label(draw, (px, py - r - 9), item["cluster_id"], FONT_SMALL, fill=(241, 255, 252), bg=(5, 12, 22, 170))
        draw.text(
            (px + r + 6, py),
            f"{item['asset_id'].split('_')[-1]}",
            font=FONT_SMALL,
            fill=(230, 242, 255),
            anchor="lm",
        )

    # Left legend
    draw.rounded_rectangle((128, 146, 560, 380), 16, fill=(9, 20, 34), outline=(74, 110, 146), width=2)
    draw_label(draw, (152, 168), "VIS-409 Greenwatch Dressing Plan", FONT_H2, anchor="la",
               fill=(225, 241, 255), bg=(8, 16, 30, 180))
    y = 214
    notes = [
        f"Placement count: {len(layout['placements'])}",
        f"Path clear radius: {layout['path_clear_radius']}",
        f"Slot clear radius: {layout['slot_clear_radius']} (+{layout['slot_hit_margin']} hit margin)",
        "No repeats if same asset closer than 1.30",
        "Scale + rotation already constrained by family rules",
        "Ready for prefab integration after Unity PlayMode regression",
    ]
    for line in notes:
        draw.text((152, y), line, font=FONT_SMALL, fill=(206, 226, 250))
        y += 25

    # Right family legend
    draw.rounded_rectangle((1165, 146, 1780, 400), 16, fill=(9, 20, 34), outline=(74, 110, 146), width=2)
    draw_label(draw, (1190, 168), "Placement families", FONT_H2, anchor="la", fill=(225, 241, 255))
    y = 208
    for family, style in FAMILY_STYLE.items():
        draw.ellipse((1200, y + 2, 1214, y + 16), fill=style["fill"], outline=style["outline"])
        draw.text((1225, y), family, font=FONT_SMALL, fill=(220, 242, 255))
        y += 24

    # Footer
    draw_label(
        draw,
        (MAP_LEFT + 22, MAP_TOP + 22),
        "No-clutter rule: road and slot zones remain readable above background dressing.",
        FONT_BODY,
        anchor="la",
        fill=(219, 244, 255),
        bg=(7, 16, 25, 175),
    )


def write_sheet(layout_path: Path, paintover_path: Path, output: Path) -> None:
    layout = json.loads(layout_path.read_text(encoding="utf-8"))
    paintover = {}
    if paintover_path.exists():
        paintover = json.loads(paintover_path.read_text(encoding="utf-8"))
    image = Image.new("RGBA", (WIDTH, HEIGHT), (2, 12, 22, 255))
    draw = ImageDraw.Draw(image)
    draw_placeholders(draw, layout, paintover)
    output.parent.mkdir(parents=True, exist_ok=True)
    image.save(output, format="PNG")
    print(f"[DC_ENV_DRESSING_RENDER] {output}")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--layout", required=True)
    parser.add_argument("--paintover-data", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()

    layout_path = Path(args.layout)
    paintover_path = Path(args.paintover_data)
    output = Path(args.output)
    write_sheet(layout_path, paintover_path, output)


if __name__ == "__main__":
    main()
