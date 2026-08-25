#!/usr/bin/env python3
"""Render NEXT-202 second-biome paint-over and kit manifest."""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any

from PIL import Image, ImageDraw, ImageFont


CAPTURED_ON = "2026-08-25"
WIDTH = 1920
HEIGHT = 1080
MAP_LEFT = 118
MAP_TOP = 132
MAP_RIGHT = 1802
MAP_BOTTOM = 820
MIN_X = -3.2
MAX_X = 25.4
MIN_Z = -2.1
MAX_Z = 13.2

PATH = [(-2, 2), (4, 2), (4, 8), (10, 8), (10, 3.5), (16, 3.5), (16, 9), (24, 9)]

SLOTS = [
    ("S1", 2.5, 2.1, "Frontline"),
    ("S2", 5.5, 2.8, "Ranged"),
    ("S3", 6.6, 5.9, "Control"),
    ("S4", 10.0, 3.3, "Frontline"),
    ("S5", 13.8, 4.4, "Ranged"),
    ("S6", 15.6, 1.0, "Control"),
    ("S7", 18.2, 4.6, "Ranged"),
    ("S8", 19.2, 7.9, "Frontline"),
]

REGIONS = [
    {
        "id": "A",
        "name": "Thaw Gate",
        "polygon": [(-2.9, -0.1), (5.2, -0.1), (5.2, 4.4), (-2.9, 4.4)],
        "color": (90, 192, 232, 72),
        "stroke": (127, 227, 255),
        "rule": "Cold fog and cracked ice frame the enemy entrance; S1 remains bright and clean.",
    },
    {
        "id": "B",
        "name": "Fen Lantern Bend",
        "polygon": [(3.6, 3.2), (12.4, 3.2), (12.4, 9.7), (3.6, 9.7)],
        "color": (130, 232, 178, 58),
        "stroke": (151, 255, 196),
        "rule": "Green ghost lanterns and reeds add identity but stay outside slot touch rings.",
    },
    {
        "id": "C",
        "name": "Glacier Keep",
        "polygon": [(13.4, 2.0), (24.9, 2.0), (24.9, 11.2), (13.4, 11.2)],
        "color": (165, 143, 255, 56),
        "stroke": (193, 178, 255),
        "rule": "Blue-violet ice cliffs reinforce the defender keep and boss silhouette contrast.",
    },
]

ASSET_KIT = [
    {
        "id": "FrostPineTall",
        "role": "north/south frame silhouette",
        "palette": "deep blue trunk, cyan snow caps",
        "budget": "≤420 tris, 1 material",
    },
    {
        "id": "IceShardCluster",
        "role": "boss/final-stand landmark accent",
        "palette": "cyan core, navy base",
        "budget": "≤280 tris, emissive optional",
    },
    {
        "id": "FenLanternPost",
        "role": "mid-bend control readability",
        "palette": "green ghost glow, dark iron",
        "budget": "≤220 tris, no shadow-casting point light on Low/Medium",
    },
    {
        "id": "FrozenRoadStone",
        "role": "road-edge rhythm",
        "palette": "desaturated slate, pale frost edge",
        "budget": "≤120 tris, atlas material",
    },
    {
        "id": "ReedSnowClump",
        "role": "low filler outside touch rings",
        "palette": "dark teal reed, muted snow",
        "budget": "≤90 tris, no collider",
    },
    {
        "id": "BrokenIceArch",
        "role": "enemy gate variant",
        "palette": "warm enemy rune inside cold arch",
        "budget": "≤700 tris, 2 materials",
    },
]


def load_font(size: int) -> ImageFont.ImageFont:
    for font_path in [
        "/System/Library/Fonts/Supplemental/Arial.ttf",
        "/System/Library/Fonts/Supplemental/Arial Bold.ttf",
        "/Library/Fonts/Arial.ttf",
    ]:
        if Path(font_path).exists():
            try:
                return ImageFont.truetype(font_path, size)
            except OSError:
                pass
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


def poly(points: list[tuple[float, float]]) -> list[tuple[int, int]]:
    return [world_to_px(x, z) for x, z in points]


def label(draw: ImageDraw.ImageDraw, xy: tuple[int, int], text: str, font: ImageFont.ImageFont,
          fill=(226, 238, 255), anchor="mm", bg=(5, 11, 20, 165)) -> None:
    box = draw.textbbox(xy, text, font=font, anchor=anchor)
    pad = 5
    draw.rounded_rectangle((box[0] - pad, box[1] - pad, box[2] + pad, box[3] + pad), 7, fill=bg)
    draw.text(xy, text, font=font, fill=fill, anchor=anchor)


def line_with_nodes(draw: ImageDraw.ImageDraw, points: list[tuple[int, int]], fill: tuple[int, int, int],
                    width: int) -> None:
    draw.line(points, fill=fill, width=width, joint="curve")
    radius = width // 2
    for x, y in points:
        draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=fill)


def draw_slot(draw: ImageDraw.ImageDraw, code: str, x: float, z: float, role: str) -> None:
    px, py = world_to_px(x, z)
    color = {
        "Frontline": (255, 212, 126),
        "Ranged": (132, 224, 255),
        "Control": (151, 255, 196),
    }[role]
    draw.ellipse((px - 31, py - 31, px + 31, py + 31), outline=color, width=5, fill=(9, 20, 34, 198))
    draw.ellipse((px - 50, py - 50, px + 50, py + 50), outline=color + (120,), width=2)
    label(draw, (px, py - 1), code, FONT_BODY, fill=(255, 255, 255))
    label(draw, (px, py + 48), role[0], FONT_TINY, fill=color)


def draw_sheet(output: Path) -> None:
    image = Image.new("RGBA", (WIDTH, HEIGHT), (7, 12, 24, 255))
    draw = ImageDraw.Draw(image, "RGBA")

    draw.rectangle((0, 0, WIDTH, HEIGHT), fill=(6, 12, 24, 255))
    draw.text((92, 42), "Frostfen Hollow · second biome paint-over", font=FONT_TITLE, fill=(235, 246, 255))
    draw.text((92, 86), "Same route/slots, colder material language, new landmark kit", font=FONT_BODY,
              fill=(174, 214, 238))

    draw.rounded_rectangle((MAP_LEFT - 20, MAP_TOP - 26, MAP_RIGHT + 20, MAP_BOTTOM + 26), 26,
                           fill=(12, 29, 45), outline=(95, 155, 196), width=2)
    draw.rectangle((MAP_LEFT, MAP_TOP, MAP_RIGHT, MAP_BOTTOM), fill=(20, 58, 70))

    for x in range(MAP_LEFT, MAP_RIGHT, 74):
        draw.line((x, MAP_TOP, x - 180, MAP_BOTTOM), fill=(64, 114, 130, 44), width=3)
    for x in range(MAP_LEFT + 30, MAP_RIGHT, 120):
        draw.line((x, MAP_BOTTOM, x + 160, MAP_TOP), fill=(120, 220, 245, 28), width=2)

    north = poly([(-3.0, 10.9), (25.0, 10.9), (25.0, 13.05), (-3.0, 13.05)])
    south = poly([(-2.7, -1.75), (24.5, -1.75), (24.5, 1.05), (-2.7, 1.05)])
    draw.polygon(north, fill=(18, 38, 70, 170))
    draw.line(north + [north[0]], fill=(113, 183, 230), width=2)
    draw.polygon(south, fill=(13, 32, 52, 170))
    draw.line(south + [south[0]], fill=(113, 183, 230), width=2)
    label(draw, (MAP_LEFT + 280, MAP_TOP + 28), "Frost pines + glacier ridge frame", FONT_SMALL)
    label(draw, (MAP_LEFT + 290, MAP_BOTTOM - 34), "Low reeds/snow foreground; never cover tap rings", FONT_SMALL)

    for region in REGIONS:
        region_points = poly(region["polygon"])
        draw.polygon(region_points, fill=region["color"])
        draw.line(region_points + [region_points[0]], fill=region["stroke"], width=3)

    path_points = [world_to_px(x, z) for x, z in PATH]
    line_with_nodes(draw, path_points, (45, 65, 78), 88)
    line_with_nodes(draw, path_points, (132, 159, 170), 58)
    line_with_nodes(draw, path_points, (213, 245, 255), 12)

    region_labels = {
        "A": world_to_px(1.4, 3.8),
        "B": world_to_px(7.6, 7.35),
        "C": world_to_px(18.3, 6.95),
    }
    for region in REGIONS:
        label(draw, region_labels[region["id"]], f"{region['id']} · {region['name']}", FONT_H2)

    for code, x, z, role in SLOTS:
        draw_slot(draw, code, x, z, role)

    landmarks = [
        ("Broken Ice Gate", -2.4, 2.0, (255, 145, 82)),
        ("Aurora Moon", -2.3, 11.9, (144, 255, 212)),
        ("Glacier Keep", 24.1, 9.0, (148, 214, 255)),
    ]
    for name, x, z, color in landmarks:
        px, py = world_to_px(x, z)
        draw.ellipse((px - 42, py - 42, px + 42, py + 42), fill=(7, 15, 28, 220), outline=color, width=5)
        label_y = py + 60 if py < MAP_TOP + 70 else py - 60
        label(draw, (px, label_y), name, FONT_SMALL, fill=color)

    kit_left, kit_top, kit_right, kit_bottom = 1225, 858, 1804, 1042
    draw.rounded_rectangle((kit_left, kit_top, kit_right, kit_bottom), 18,
                           fill=(10, 22, 35), outline=(88, 150, 200), width=2)
    draw.text((kit_left + 18, kit_top + 16), "Frostfen modular kit", font=FONT_H2, fill=(224, 239, 255))
    y = kit_top + 54
    for asset in ASSET_KIT:
        draw.text((kit_left + 20, y), f"• {asset['id']} — {asset['role']}", font=FONT_SMALL,
                  fill=(204, 229, 247))
        y += 22

    strip_left, strip_top, strip_right, strip_bottom = 120, 858, 1170, 1042
    draw.rounded_rectangle((strip_left, strip_top, strip_right, strip_bottom), 18,
                           fill=(10, 22, 35), outline=(88, 150, 200), width=2)
    draw.text((strip_left + 18, strip_top + 16), "Readability rule", font=FONT_H2, fill=(224, 239, 255))
    notes = [
        "Cold palette shifts the mood; route/slot value hierarchy stays identical.",
        "Enemy energy remains warm so threat direction is not lost in blue ice.",
        "No tall kit piece enters S1-S8 touch rings or boss telegraph space.",
        "Low/Medium profiles use emissive textures, not shadow-casting point lights.",
    ]
    y = strip_top + 56
    for note in notes:
        draw.text((strip_left + 20, y), f"• {note}", font=FONT_SMALL, fill=(204, 229, 247))
        y += 25

    output.parent.mkdir(parents=True, exist_ok=True)
    image.convert("RGB").save(output, quality=94)


def write_data(path: Path) -> None:
    data: dict[str, Any] = {
        "schema": "defender-chronicles.visual-development.second-biome-paintover.v1",
        "captured_on": CAPTURED_ON,
        "todo": "NEXT-202",
        "biome": "Frostfen Hollow",
        "scope": "Preproduction paint-over and modular kit manifest; no runtime map, route, collider or camera change.",
        "route_reuse": [{"x": x, "z": z} for x, z in PATH],
        "slot_reuse": [{"code": code, "x": x, "z": z, "role": role} for code, x, z, role in SLOTS],
        "regions": [
            {"id": item["id"], "name": item["name"], "polygon": item["polygon"], "rule": item["rule"]}
            for item in REGIONS
        ],
        "asset_kit": ASSET_KIT,
        "acceptance_notes": [
            "Proves the current fantasy material language can scale beyond Greenwatch Valley.",
            "Keeps eight slot/touch circles, path hierarchy and boss read rules intact.",
            "Leaves Android/iOS device acceptance under their own gates.",
        ],
    }
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--data", type=Path, required=True)
    args = parser.parse_args()

    draw_sheet(args.output)
    write_data(args.data)
    print(args.output)
    print(args.data)
    print("[DC_SECOND_BIOME_PAINTOVER_PASS]")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
