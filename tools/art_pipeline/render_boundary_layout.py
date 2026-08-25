#!/usr/bin/env python3
"""Render VIS-410 boundary dressing placement manifest as a top-down plan sheet.

Usage:
  python3 tools/art_pipeline/render_boundary_layout.py -- \
    --layout docs/evidence/visual-development/boundary-layout-2026-08-16.json \
    --paintover-data docs/evidence/visual-development/greenwatch-valley-paintover-plan-2026-08-16.json \
    --output docs/evidence/visual-development/boundary-layout-2026-08-16.png
"""

from __future__ import annotations

import argparse
import json
import math
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
    "RidgeWall": {
        "fill": (158, 186, 214, 230),
        "outline": (224, 240, 255, 252),
        "radius": 18,
        "label": "Ridge",
    },
    "ForegroundStrip": {
        "fill": (110, 122, 124, 215),
        "outline": (221, 236, 244, 235),
        "radius": 14,
        "label": "FG Strip",
    },
    "Silhouette": {
        "fill": (91, 108, 146, 205),
        "outline": (188, 208, 238, 245),
        "radius": 16,
        "label": "Silhouette",
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


def draw_label(draw: ImageDraw.ImageDraw, xy: tuple[int, int], text: str,
               font: ImageFont.ImageFont, fill=(226, 238, 255), anchor="mm", bg=None) -> None:
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
    draw.ellipse((px - 28, py - 28, px + 28, py + 28), outline=color, width=4, fill=(11, 20, 36, 188))
    draw.ellipse((px - 48, py - 48, px + 48, py + 48), outline=color + (96,), width=2)
    draw_label(draw, (px, py - 2), code, FONT_BODY, fill=(255, 255, 255), bg=(8, 15, 25, 175))
    draw_label(draw, (px, py + 45), role[0], FONT_SMALL, fill=color, bg=(8, 15, 25, 165))


def draw_placement(draw: ImageDraw.ImageDraw, item: dict[str, Any], bounds: dict[str, float]) -> None:
    style = FAMILY_STYLE.get(item["family"], {
        "fill": (178, 178, 178, 200),
        "outline": (245, 245, 245, 238),
        "radius": 8,
        "label": "X",
    })
    px, py = world_to_px(float(item["x"]), float(item["z"]), bounds)
    r = style["radius"]
    rect = (px - r, py - r, px + r, py + r)
    draw.ellipse(rect, fill=style["fill"], outline=style["outline"], width=2)
    variant = str(item["asset_id"]).split("_")[-1]
    draw_label(draw, (px, py - r - 12), item["cluster_id"], FONT_SMALL, fill=(236, 245, 255),
               bg=(8, 16, 28, 165))
    draw.text((px + r + 3, py - 1), variant, font=FONT_SMALL, fill=(226, 239, 255), anchor="lm")
    ang = item.get("rotation_y", 0.0)
    if abs(float(ang)) > 0.01:
        bar = 10 + int(r * 0.4)
        endx = px + int(bar * math.cos(math.radians(float(ang))))
        endz = py + int(bar * math.sin(math.radians(float(ang))))
        draw.line((px, py, endx, endz), fill=style["outline"][:3] + (180,), width=2)


def draw_noisy_regions(draw: ImageDraw.ImageDraw, paintover: dict[str, Any], bounds: dict[str, float]) -> None:
    if paintover.get("noise_bands"):
        for band in paintover["noise_bands"]:
            p1 = world_to_px(band["min_x"], band["min_z"], bounds)
            p2 = world_to_px(band["max_x"], band["max_z"], bounds)
            draw.rounded_rectangle(
                (min(p1[0], p2[0]), min(p1[1], p2[1]), max(p1[0], p2[0]), max(p1[1], p2[1])),
                8, outline=(111, 154, 200), width=2, fill=(24, 40, 60, 36)
            )


def draw_bounds(read_data: dict[str, Any], draw: ImageDraw.ImageDraw, paintover: dict[str, Any],
                metrics: dict[str, Any]) -> None:
    pass


def draw_placeholders(draw: ImageDraw.ImageDraw, layout: dict[str, Any], paintover: dict[str, Any]) -> None:
    bounds = layout.get("world_bounds", {
        "min_x": DEFAULT_MIN_X,
        "max_x": DEFAULT_MAX_X,
        "min_z": DEFAULT_MIN_Z,
        "max_z": DEFAULT_MAX_Z,
    })

    draw.rounded_rectangle((MAP_LEFT - 20, MAP_TOP - 26, MAP_RIGHT + 20, MAP_BOTTOM + 26), 26,
                           fill=(12, 26, 34), outline=(76, 114, 143), width=2)
    draw.rectangle((MAP_LEFT, MAP_TOP, MAP_RIGHT, MAP_BOTTOM), fill=(15, 54, 44))

    if paintover.get("regions"):
        for region in paintover["regions"]:
            points = [world_to_px(point["x"], point["z"], bounds) for point in region.get("polygon", [])]
            if len(points) >= 3:
                draw.polygon(points, fill=(80, 110, 145, 28), outline=(150, 180, 220), width=2)

    draw_noisy_regions(draw, paintover, bounds)

    path = [(entry["x"], entry["z"]) for entry in layout["path"]]
    path_px = [world_to_px(x, z, bounds) for x, z in path]
    draw_polyline(draw, path_px, (86, 96, 103), 86)
    draw_polyline(draw, path_px, (128, 91, 57), 60)
    draw_polyline(draw, path_px, (175, 124, 86), 12)

    for slot in layout["slots"]:
        slot_radius = (layout["slot_clear_radius"] + layout["slot_hit_margin"] + 0.4) * 28
        px, py = world_to_px(slot["x"], slot["z"], bounds)
        draw.ellipse((px - slot_radius, py - slot_radius, px + slot_radius, py + slot_radius),
                     outline=(255, 232, 128, 150), width=2)

    for item in layout["placements"]:
        draw_placement(draw, item, bounds)
        px, py = world_to_px(float(item["x"]), float(item["z"]), bounds)
        family = item["family"]
        for cluster_name in (item["cluster_id"],):
            draw_label(draw, (px, py + 9), cluster_name, FONT_SMALL, fill=(200, 222, 246), bg=(7, 15, 24, 150))

    # slot rings
    for slot in layout["slots"]:
        draw_slot(draw, slot["code"], slot["x"], slot["z"], slot["role"], bounds)

    # Legend + notes
    draw.rounded_rectangle((128, 146, 560, 390), 16, fill=(10, 20, 35), outline=(74, 110, 146), width=2)
    draw_label(draw, (152, 168), "VIS-410 Greenwatch Boundary Plan", FONT_H2, anchor="la",
               fill=(225, 241, 255), bg=(8, 16, 30, 180))
    y = 214
    notes = [
        f"Placement count: {len(layout['placements'])}",
        "Boundary families: RidgeWall / ForegroundStrip / Silhouette",
        f"Path clear radius: {layout['path_clear_radius']}",
        f"Slot clear radius: {layout['slot_clear_radius']} (+{layout['slot_hit_margin']} hit margin)",
        "Repeat only flagged on same asset_id within 1.50",
        "Prefab integration + device captures are open gates."
    ]
    for line in notes:
        draw.text((152, y), line, font=FONT_SMALL, fill=(206, 226, 250))
        y += 24

    draw.rounded_rectangle((1164, 146, 1780, 420), 16, fill=(10, 20, 35), outline=(74, 110, 146), width=2)
    draw_label(draw, (1190, 168), "Placement families", FONT_H2, anchor="la", fill=(225, 241, 255))
    y = 214
    for family, style in FAMILY_STYLE.items():
        draw.ellipse((1200, y + 2, 1214, y + 16), fill=style["fill"], outline=style["outline"])
        draw.text((1225, y), f"{family} ({style['label']})", font=FONT_SMALL, fill=(220, 242, 255))
        y += 24

    draw_label(
        draw,
        (MAP_LEFT + 22, MAP_TOP + 22),
        "Goal: frame map edges with distant silhouettes while preserving route and slot readability.",
        FONT_BODY,
        anchor="la",
        fill=(219, 244, 255),
        bg=(7, 16, 25, 175),
    )


def write_sheet(layout_path: Path, paintover_path: Path, output: Path) -> None:
    layout = json.loads(layout_path.read_text(encoding="utf-8"))
    paintover = json.loads(paintover_path.read_text(encoding="utf-8")) if paintover_path.exists() else {}
    image = Image.new("RGBA", (WIDTH, HEIGHT), (2, 12, 22, 255))
    draw = ImageDraw.Draw(image)
    draw_placeholders(draw, layout, paintover)
    output.parent.mkdir(parents=True, exist_ok=True)
    image.save(output, format="PNG")
    print(f"[DC_BOUNDARY_LAYOUT_RENDER] {output}")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--layout", required=True)
    parser.add_argument("--paintover-data", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()

    write_sheet(Path(args.layout), Path(args.paintover_data), Path(args.output))


if __name__ == "__main__":
    main()
