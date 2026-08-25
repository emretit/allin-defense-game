#!/usr/bin/env python3
"""Create VIS-813 accessibility and VIS-814 comparison evidence from accepted runtime captures."""

from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path
from statistics import mean

from PIL import Image, ImageDraw, ImageFont, ImageOps, ImageStat


ROOT = Path(__file__).resolve().parents[2]
RUNTIME = ROOT / "docs" / "evidence" / "runtime"
VISUAL = ROOT / "docs" / "evidence" / "visual-development"
STAMP = "2026-08-17"
ACCEPTED = "vis812-final-20260817-191207"


@dataclass(frozen=True)
class Capture:
    state: str
    path: Path


def font(size: int) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    candidates = [
        "/System/Library/Fonts/Supplemental/Arial Bold.ttf",
        "/System/Library/Fonts/Supplemental/Arial.ttf",
        "/System/Library/Fonts/Helvetica.ttc",
    ]
    for candidate in candidates:
        try:
            return ImageFont.truetype(candidate, size)
        except OSError:
            pass
    return ImageFont.load_default()


TITLE = font(30)
LABEL = font(20)
SMALL = font(15)


def load_captures() -> list[Capture]:
    states = [
        "preparation",
        "ability",
        "normal",
        "dense",
        "boss1",
        "boss2",
        "boss3",
        "victory",
        "defeat",
    ]
    captures: list[Capture] = []
    for state in states:
        path = RUNTIME / f"defender-simulator-{ACCEPTED}-{state}-landscape-upright.png"
        if not path.exists():
            raise FileNotFoundError(path)
        captures.append(Capture(state, path))
    return captures


def fit(image: Image.Image, size: tuple[int, int]) -> Image.Image:
    return ImageOps.contain(image.convert("RGB"), size, Image.Resampling.LANCZOS)


def draw_header(draw: ImageDraw.ImageDraw, text: str, xy: tuple[int, int]) -> None:
    x, y = xy
    draw.text((x + 2, y + 2), text, fill=(0, 0, 0), font=TITLE)
    draw.text((x, y), text, fill=(245, 248, 250), font=TITLE)


def draw_label(draw: ImageDraw.ImageDraw, text: str, xy: tuple[int, int]) -> None:
    x, y = xy
    draw.rounded_rectangle((x, y, x + 198, y + 34), radius=4, fill=(10, 18, 24))
    draw.text((x + 10, y + 7), text, fill=(245, 248, 250), font=LABEL)


def grayscale_sheet(captures: list[Capture]) -> Path:
    thumb = (398, 224)
    margin = 28
    gap = 18
    header = 82
    cols = 3
    rows = 3
    width = margin * 2 + cols * thumb[0] + (cols - 1) * gap
    height = header + margin + rows * (thumb[1] + 42) + (rows - 1) * gap
    canvas = Image.new("RGB", (width, height), (17, 23, 27))
    draw = ImageDraw.Draw(canvas)
    draw_header(draw, "VIS-813 grayscale readability - accepted VIS-812 states", (margin, 24))

    for index, capture in enumerate(captures):
        col = index % cols
        row = index // cols
        x = margin + col * (thumb[0] + gap)
        y = header + row * (thumb[1] + 42 + gap)
        image = Image.open(capture.path)
        gray = fit(ImageOps.grayscale(image).convert("RGB"), thumb)
        canvas.paste(gray, (x, y + 38))
        draw_label(draw, capture.state.upper(), (x, y))
        draw.rectangle((x, y + 38, x + gray.width - 1, y + 38 + gray.height - 1), outline=(92, 116, 130), width=2)

    VISUAL.mkdir(parents=True, exist_ok=True)
    output = VISUAL / f"vis813-accessibility-grayscale-contact-sheet-{STAMP}.png"
    canvas.save(output)
    return output


def comparison_sheet(captures: list[Capture]) -> Path:
    before_path = RUNTIME / "defender-simulator-run-late-landscape-review-readable-2026-08-16.png"
    final_prep = next(item for item in captures if item.state == "preparation").path
    final_dense = next(item for item in captures if item.state == "dense").path
    final_victory = next(item for item in captures if item.state == "victory").path
    items = [
        ("BEFORE partial runtime", before_path),
        ("FINAL preparation", final_prep),
        ("FINAL dense wave", final_dense),
        ("FINAL victory", final_victory),
    ]
    for _, path in items:
        if not path.exists():
            raise FileNotFoundError(path)

    thumb = (592, 333)
    margin = 30
    gap = 24
    header = 82
    width = margin * 2 + 2 * thumb[0] + gap
    height = header + margin + 2 * (thumb[1] + 42) + gap
    canvas = Image.new("RGB", (width, height), (16, 20, 24))
    draw = ImageDraw.Draw(canvas)
    draw_header(draw, "VIS-814 before/after visual comparison", (margin, 24))

    for index, (label, path) in enumerate(items):
        col = index % 2
        row = index // 2
        x = margin + col * (thumb[0] + gap)
        y = header + row * (thumb[1] + 42 + gap)
        image = fit(Image.open(path), thumb)
        canvas.paste(image, (x, y + 38))
        draw_label(draw, label, (x, y))
        draw.rectangle((x, y + 38, x + image.width - 1, y + 38 + image.height - 1), outline=(104, 127, 142), width=2)

    output = VISUAL / f"vis814-before-after-contact-sheet-{STAMP}.png"
    canvas.save(output)
    return output


def image_metrics(path: Path) -> dict[str, object]:
    image = Image.open(path).convert("RGB")
    gray = ImageOps.grayscale(image)
    stat = ImageStat.Stat(gray)
    width, height = image.size
    center = gray.crop((width * 0.18, height * 0.12, width * 0.82, height * 0.86))
    center_stat = ImageStat.Stat(center)
    histogram = gray.histogram()
    total = sum(histogram)
    low = sum(histogram[:32]) / total
    high = sum(histogram[224:]) / total
    return {
        "path": str(path.relative_to(ROOT)),
        "width": width,
        "height": height,
        "luminanceMean": round(stat.mean[0], 2),
        "luminanceStdDev": round(stat.stddev[0], 2),
        "centerStdDev": round(center_stat.stddev[0], 2),
        "darkPixelShare": round(low, 4),
        "brightPixelShare": round(high, 4),
    }


def write_report(captures: list[Capture], grayscale: Path, comparison: Path) -> Path:
    metrics = [{"state": capture.state, **image_metrics(capture.path)} for capture in captures]
    report = {
        "date": STAMP,
        "acceptedStamp": ACCEPTED,
        "vis813": {
            "status": "in_review",
            "grayscaleContactSheet": str(grayscale.relative_to(ROOT)),
            "statesChecked": [capture.state for capture in captures],
            "allCapturesLandscape1334x750": all(
                item["width"] == 1334 and item["height"] == 750 for item in metrics
            ),
            "luminanceStdDevAverage": round(mean(item["luminanceStdDev"] for item in metrics), 2),
            "notes": [
                "Critical state labels, result overlays and major silhouettes remain distinguishable in grayscale contact sheet.",
                "This is visual accessibility evidence; real assistive-tech/VoiceOver validation remains separate.",
            ],
        },
        "vis814": {
            "status": "in_review",
            "beforeAfterContactSheet": str(comparison.relative_to(ROOT)),
            "baseline": "docs/evidence/runtime/defender-simulator-run-late-landscape-review-readable-2026-08-16.png",
            "finalStates": ["preparation", "dense", "victory"],
            "notes": [
                "Comparison shows final HUD/material/environment/slot/readability progression against the earlier partial runtime baseline.",
                "Final full-state evidence is the VIS-812 accepted set.",
            ],
        },
        "metrics": metrics,
    }
    output = VISUAL / f"vis813-814-visual-audit-report-{STAMP}.json"
    output.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    return output


def main() -> None:
    captures = load_captures()
    grayscale = grayscale_sheet(captures)
    comparison = comparison_sheet(captures)
    report = write_report(captures, grayscale, comparison)
    print(grayscale.relative_to(ROOT))
    print(comparison.relative_to(ROOT))
    print(report.relative_to(ROOT))


if __name__ == "__main__":
    main()
