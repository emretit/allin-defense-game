#!/usr/bin/env python3
"""Render VIS-401 Greenwatch Valley paint-over planning sheet.

The output is intentionally a planning artifact, not runtime art. It maps the
current route, eight outpost slots, landmark hierarchy and no-clutter gameplay
lanes so later environment work does not damage pathing, slot tapping or HUD
readability.

Usage:
  python3 tools/art_pipeline/render_environment_paintover_plan.py \
    --output docs/evidence/visual-development/greenwatch-valley-paintover-plan-2026-08-16.png \
    --data docs/evidence/visual-development/greenwatch-valley-paintover-plan-2026-08-16.json
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any

from PIL import Image, ImageDraw, ImageFont


CAPTURED_ON = "2026-08-16"
WIDTH = 1920
HEIGHT = 1080
MAP_LEFT = 118
MAP_TOP = 132
MAP_RIGHT = 1802
MAP_BOTTOM = 824
MIN_X = -3.2
MAX_X = 25.4
MIN_Z = -2.1
MAX_Z = 13.2

PATH = [(-2, 2), (4, 2), (4, 8), (10, 8), (10, 3.5), (16, 3.5), (16, 9), (24, 9)]

SLOTS = [
    ("S1", "slot_valley_west_entry", 2.5, 2.1, "Frontline"),
    ("S2", "slot_valley_west_ridge", 5.5, 2.8, "Ranged"),
    ("S3", "slot_valley_north_bend", 6.6, 5.9, "Control"),
    ("S4", "slot_valley_mid_cross", 10.0, 3.3, "Frontline"),
    ("S5", "slot_valley_east_ridge", 13.8, 4.4, "Ranged"),
    ("S6", "slot_valley_gate_low", 15.6, 1.0, "Control"),
    ("S7", "slot_valley_gate_high", 18.2, 4.6, "Ranged"),
    ("S8", "slot_valley_final_stand", 19.2, 7.9, "Frontline"),
]

COMMAND_POINTS = [
    ("Guardian", 4.0, 1.4, "Knight hold"),
    ("Mid Hold", 10.8, 4.3, "Mid combat timing"),
    ("Gate Hold", 16.9, 2.4, "Late hold"),
    ("Boss Focus", 18.8, 6.8, "Boss pressure"),
]

REGIONS = [
    {
        "id": "A",
        "name": "West Entry Tutorial",
        "polygon": [(-2.9, -0.1), (5.2, -0.1), (5.2, 4.4), (-2.9, 4.4)],
        "color": (237, 151, 81, 72),
        "stroke": (237, 151, 81),
        "rule": "Gate, first slot and route must stay clean; use warm enemy embers and sparse rocks.",
    },
    {
        "id": "B",
        "name": "Mid Control Bend",
        "polygon": [(3.6, 3.2), (12.4, 3.2), (12.4, 9.7), (3.6, 9.7)],
        "color": (86, 181, 255, 60),
        "stroke": (86, 181, 255),
        "rule": "Best place for low ground variation and small readable prop clusters away from slot circles.",
    },
    {
        "id": "C",
        "name": "Final Stand / Boss Read",
        "polygon": [(13.4, 2.0), (24.9, 2.0), (24.9, 11.2), (13.4, 11.2)],
        "color": (155, 123, 255, 56),
        "stroke": (155, 123, 255),
        "rule": "Keep boss approach, final slots and castle silhouette dominant; no tall props in front of keep.",
    },
]

NOISE_BANDS = [
    ("North frame prop band", -3.0, 10.9, 25.0, 13.05, "High silhouettes: pines, rocks, ridge glow."),
    ("South foreground frame", -2.7, -1.75, 24.5, 1.05, "Foreground parallax silhouettes, low near path."),
]

LANDMARKS = [
    ("Enemy Gate", -2.4, 2.0, "warm red/orange portal + entry spikes"),
    ("Moon/Ridge", -2.8, 13.1, "cool moon and ridge silhouette"),
    ("Defender Keep", 24.1, 9.0, "blue stone castle, strongest friendly mass"),
]


def load_font(size: int) -> ImageFont.ImageFont:
    candidates = [
        "/System/Library/Fonts/Supplemental/Arial.ttf",
        "/System/Library/Fonts/Supplemental/Arial Bold.ttf",
        "/Library/Fonts/Arial.ttf",
    ]
    for path in candidates:
        if Path(path).exists():
            try:
                return ImageFont.truetype(path, size)
            except OSError:
                continue
    return ImageFont.load_default()


FONT_TITLE = load_font(34)
FONT_H2 = load_font(23)
FONT_BODY = load_font(18)
FONT_SMALL = load_font(15)
FONT_TINY = load_font(13)


def world_to_px(x: float, z: float) -> tuple[int, int]:
    px = MAP_LEFT + (x - MIN_X) / (MAX_X - MIN_X) * (MAP_RIGHT - MAP_LEFT)
    py = MAP_BOTTOM - (z - MIN_Z) / (MAX_Z - MIN_Z) * (MAP_BOTTOM - MAP_TOP)
    return round(px), round(py)


def polygon_px(points: list[tuple[float, float]]) -> list[tuple[int, int]]:
    return [world_to_px(x, z) for x, z in points]


def draw_label(draw: ImageDraw.ImageDraw, xy: tuple[int, int], text: str, font: ImageFont.ImageFont,
               fill=(226, 238, 255), anchor="mm", bg=None) -> None:
    if bg is not None:
        box = draw.textbbox(xy, text, font=font, anchor=anchor)
        pad = 5
        draw.rounded_rectangle((box[0] - pad, box[1] - pad, box[2] + pad, box[3] + pad), 7, fill=bg)
    draw.text(xy, text, font=font, fill=fill, anchor=anchor)


def draw_polyline(draw: ImageDraw.ImageDraw, points: list[tuple[int, int]], fill: tuple[int, int, int],
                  width: int, joint="curve") -> None:
    draw.line(points, fill=fill, width=width, joint=joint)
    radius = width // 2
    for x, y in points:
        draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=fill)


def draw_slot(draw: ImageDraw.ImageDraw, code: str, x: float, z: float, role: str) -> None:
    px, py = world_to_px(x, z)
    color = {
        "Frontline": (255, 191, 92),
        "Ranged": (118, 184, 255),
        "Control": (123, 231, 193),
    }[role]
    draw.ellipse((px - 30, py - 30, px + 30, py + 30), outline=color, width=5, fill=(12, 24, 38, 190))
    draw.ellipse((px - 50, py - 50, px + 50, py + 50), outline=color + (110,), width=2)
    draw_label(draw, (px, py - 2), code, FONT_BODY, fill=(255, 255, 255), bg=(8, 15, 25, 180))
    draw_label(draw, (px, py + 47), role[0], FONT_TINY, fill=color, bg=(8, 15, 25, 165))


def draw_topdown_plan(draw: ImageDraw.ImageDraw) -> None:
    draw.rounded_rectangle((MAP_LEFT - 20, MAP_TOP - 26, MAP_RIGHT + 20, MAP_BOTTOM + 26), 26,
                           fill=(12, 26, 34), outline=(76, 114, 143), width=2)
    draw.rectangle((MAP_LEFT, MAP_TOP, MAP_RIGHT, MAP_BOTTOM), fill=(17, 64, 50))

    for name, x1, z1, x2, z2, rule in NOISE_BANDS:
        px1, py1 = world_to_px(x1, z1)
        px2, py2 = world_to_px(x2, z2)
        draw.rounded_rectangle((min(px1, px2), min(py1, py2), max(px1, px2), max(py1, py2)), 15,
                               fill=(22, 39, 63, 155), outline=(88, 128, 172), width=2)
        draw_label(draw, ((px1 + px2) // 2, min(py1, py2) + 22), name, FONT_SMALL,
                   fill=(190, 218, 255), bg=(5, 12, 22, 140))

    for region in REGIONS:
        draw.polygon(polygon_px(region["polygon"]), fill=region["color"])
        draw.line(polygon_px(region["polygon"]) + [polygon_px(region["polygon"])[0]], fill=region["stroke"], width=3)
        cx = sum(point[0] for point in polygon_px(region["polygon"])) // len(region["polygon"])
        cy = sum(point[1] for point in polygon_px(region["polygon"])) // len(region["polygon"])
        draw_label(draw, (cx, cy), f"{region['id']} · {region['name']}", FONT_H2, bg=(6, 12, 22, 165))

    path_px = [world_to_px(x, z) for x, z in PATH]
    draw_polyline(draw, path_px, (86, 96, 103), 84)
    draw_polyline(draw, path_px, (128, 91, 57), 58)
    draw_polyline(draw, path_px, (165, 122, 82), 12)

    for (name, x, z, detail) in LANDMARKS:
        px, py = world_to_px(x, z)
        draw.ellipse((px - 40, py - 40, px + 40, py + 40), fill=(14, 25, 37, 215),
                     outline=(255, 178, 85) if "Gate" in name else (120, 194, 255), width=4)
        label_y = py + 58 if py < MAP_TOP + 65 else py - 58
        draw_label(draw, (px, label_y), name, FONT_SMALL, fill=(255, 232, 184), bg=(8, 12, 20, 170))

    for code, slot_id, x, z, role in SLOTS:
        draw_slot(draw, code, x, z, role)

    for name, x, z, detail in COMMAND_POINTS:
        px, py = world_to_px(x, z)
        draw.rectangle((px - 13, py - 13, px + 13, py + 13), fill=(236, 242, 255), outline=(10, 20, 30), width=2)
        draw_label(draw, (px, py + 31), name, FONT_TINY, fill=(224, 233, 255), bg=(8, 12, 20, 150))

    draw_label(draw, (MAP_LEFT + 22, MAP_TOP + 22), "No-clutter rule: road edge + slot radius stays readable",
               FONT_BODY, anchor="lm", fill=(219, 244, 255), bg=(7, 16, 25, 175))


def draw_isometric_strip(draw: ImageDraw.ImageDraw) -> None:
    left, top, right, bottom = 120, 868, 1170, 1032
    draw.rounded_rectangle((left, top, right, bottom), 18, fill=(10, 22, 35), outline=(66, 105, 146), width=2)
    draw_label(draw, (left + 18, top + 18), "Isometric intent strip", FONT_H2, anchor="la",
               fill=(222, 239, 255))
    base_y = top + 116
    scale_x = 36
    scale_z = 9

    def iso(x: float, z: float) -> tuple[int, int]:
        return round(left + 92 + x * scale_x + z * 7), round(base_y - z * scale_z)

    path_points = [iso(x + 2, z) for x, z in PATH]
    draw_polyline(draw, path_points, (80, 90, 99), 24)
    draw_polyline(draw, path_points, (142, 97, 63), 14)
    for code, _slot_id, x, z, role in SLOTS:
        px, py = iso(x + 2, z)
        draw.ellipse((px - 9, py - 9, px + 9, py + 9), fill=(115, 203, 255), outline=(255, 255, 255))
    for name, x, z, detail in LANDMARKS:
        px, py = iso(x + 2, z)
        draw.polygon([(px, py - 30), (px - 20, py + 15), (px + 20, py + 15)], fill=(90, 145, 216))
    draw_label(draw, (left + 840, top + 48), "Depth order: foreground frame → battlefield → north ridge/moon",
               FONT_SMALL, fill=(185, 214, 244), bg=(6, 13, 22, 120))


def draw_legend(draw: ImageDraw.ImageDraw) -> None:
    left, top, right, bottom = 1225, 858, 1800, 1044
    draw.rounded_rectangle((left, top, right, bottom), 18, fill=(10, 22, 35), outline=(66, 105, 146), width=2)
    draw_label(draw, (left + 18, top + 18), "Implementation notes", FONT_H2, anchor="la")
    notes = [
        "• Keep all tall props outside slot/tap circles.",
        "• Road and slots must beat grass/noise in value.",
        "• 3 landmarks: Enemy Gate, Moon/Ridge, Defender Keep.",
        "• North/south bands carry silhouette variety.",
        "• No colliders or gameplay changes in VIS-401.",
    ]
    y = top + 56
    for note in notes:
        draw.text((left + 20, y), note, font=FONT_SMALL, fill=(207, 226, 246))
        y += 23


def write_data(path: Path) -> None:
    data: dict[str, Any] = {
        "schema": "allin-defense.visual-development.greenwatch-paintover-plan.v1",
        "captured_on": CAPTURED_ON,
        "todo": "VIS-401",
        "scope": "Planning artifact only; does not change runtime route, slot positions, colliders or camera.",
        "world_bounds": {"min_x": MIN_X, "max_x": MAX_X, "min_z": MIN_Z, "max_z": MAX_Z},
        "path": [{"x": x, "z": z} for x, z in PATH],
        "slots": [
            {"code": code, "id": slot_id, "x": x, "z": z, "role": role}
            for code, slot_id, x, z, role in SLOTS
        ],
        "command_points": [
            {"name": name, "x": x, "z": z, "purpose": detail}
            for name, x, z, detail in COMMAND_POINTS
        ],
        "regions": [
            {
                "id": region["id"],
                "name": region["name"],
                "polygon": [{"x": x, "z": z} for x, z in region["polygon"]],
                "rule": region["rule"],
            }
            for region in REGIONS
        ],
        "noise_bands": [
            {"name": name, "min_x": x1, "min_z": z1, "max_x": x2, "max_z": z2, "rule": rule}
            for name, x1, z1, x2, z2, rule in NOISE_BANDS
        ],
        "landmarks": [
            {"name": name, "x": x, "z": z, "visual_role": detail}
            for name, x, z, detail in LANDMARKS
        ],
    }
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, indent=2), encoding="utf-8")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True)
    parser.add_argument("--data", required=True)
    args = parser.parse_args()

    image = Image.new("RGB", (WIDTH, HEIGHT), (5, 10, 18))
    draw = ImageDraw.Draw(image, "RGBA")
    draw.rectangle((0, 0, WIDTH, HEIGHT), fill=(6, 12, 22))
    draw_label(draw, (WIDTH // 2, 44), "Greenwatch Valley VIS-401 Paint-over Plan", FONT_TITLE,
               fill=(232, 244, 255))
    draw_label(draw, (WIDTH // 2, 78),
               "Route, 8 slots and keep/gate hierarchy are preserved; art noise is pushed into controlled bands.",
               FONT_BODY, fill=(168, 202, 232))
    draw_topdown_plan(draw)
    draw_isometric_strip(draw)
    draw_legend(draw)

    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    image.save(output)
    write_data(Path(args.data))
    print(f"[DC_GREENWATCH_PAINTOVER_PLAN] {output}")
    print(f"[DC_GREENWATCH_PAINTOVER_DATA] {args.data}")


if __name__ == "__main__":
    main()
