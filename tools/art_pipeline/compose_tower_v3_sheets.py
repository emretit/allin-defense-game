"""Compose tower v3 review sheets (needs Pillow). Usage: python compose_tower_v3_sheets.py a1|a2"""

import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Builds" / "towers-v3"
BATTLE = ROOT / "Builds" / "step6-after-chapter1-wave-1280x960.png"
BG = (9, 19, 29)
INK = (233, 238, 245)
MUTED = (150, 166, 186)
GOLD = (242, 193, 78)


def font(size, bold=False):
    name = "NotoSans-Bold.ttf" if bold else "NotoSans-Regular.ttf"
    return ImageFont.truetype(str(ROOT / "Assets" / "Art" / "Fonts" / name), size)


def game_strip(images, slots, crop):
    """Paste transparent game-scale renders (origin = image centre) onto the real battle screenshot."""
    battle = Image.open(BATTLE).convert("RGBA")
    for im, (sx, sy) in zip(images, slots):
        tile = Image.open(im).convert("RGBA")
        battle.alpha_composite(tile, (int(sx - tile.width / 2), int(sy - tile.height / 2)))
    return battle.crop(crop).convert("RGB")


def a1():
    src = OUT / "a1"
    styles = [
        ("1  YUMUŞAK STİLİZE", "oyuncak kalitesi · yuvarlak hatlar, tombul pahlar, sıcak doygun renk"),
        ("2  ZARİF MİNİMAL", "temiz geometri · sekizgen kesimler, ince altın çizgi, az parça"),
        ("3  ZENGİN FANTASTİK", "ince detay · taş sıraları, ahşap payanda, çatı kaburgası, sancak"),
    ]
    kinds = [("archer", "OKÇU"), ("mage", "BÜYÜCÜ"), ("barracks", "KIŞLA")]
    tile = 360
    pad = 24
    game_w, game_h = 470, 360
    head = 120
    row_h = tile + 64
    width = pad + 3 * (tile + 12) + game_w + pad
    height = head + 3 * row_h + 40
    sheet = Image.new("RGB", (width, height), BG)
    d = ImageDraw.Draw(sheet)
    d.text((pad, 22), "KULELER V3  ·  STİL SEÇİMİ", font=font(34, True), fill=GOLD)
    d.text((pad, 68), "Seviye 1 · oyun kamerası 43° ortografik · aynı ışık (ay anahtar ışığı + gece ortamı) · "
           "tek ortak malzeme + renk atlası", font=font(17), fill=MUTED)
    gx = pad + 3 * (tile + 12)
    for r, (title, sub) in enumerate(styles):
        y = head + r * row_h
        d.text((pad, y), title, font=font(22, True), fill=INK)
        d.text((pad + 290, y + 5), sub, font=font(16), fill=MUTED)
        y += 34
        for c, (kind, label) in enumerate(kinds):
            im = Image.open(src / f"s{r + 1}_{kind}.png").convert("RGB").resize((tile, tile), Image.LANCZOS)
            x = pad + c * (tile + 12)
            sheet.paste(im, (x, y))
            d.text((x + 10, y + tile - 28), label, font=font(16, True), fill=INK)
        strip = game_strip([src / f"s{r + 1}_{k}_game960.png" for k, _ in kinds],
                           [(519, 590), (738, 590), (843, 433)], (430, 300, 430 + game_w, 300 + game_h))
        sheet.paste(strip, (gx, y))
        d.rectangle((gx, y, gx + game_w - 1, y + game_h - 1), outline=(60, 78, 100))
        d.text((gx + 8, y + game_h - 26), "oyun ölçeği 1280×960 (1:1 piksel)", font=font(14), fill=INK)
    d.text((pad, height - 30), "Sağ sütun: kuleler gerçek savaş ekran görüntüsündeki yuvalara 1:1 piksel ölçekle yerleştirildi "
           "(okçu · büyücü · kışla); eski kuleden ~%35 büyük.", font=font(14), fill=MUTED)
    sheet.save(OUT / "style-options.png")
    print(OUT / "style-options.png")


KINDS = [("archer", "OKÇU", ["L1", "L2", "RapidFire", "Sharpshooter"]),
         ("mage", "BÜYÜCÜ", ["L1", "L2", "Blast", "Frost"]),
         ("barracks", "KIŞLA", ["L1", "L2", "ShieldGuard", "AssaultGuard"])]
LABELS = {"L1": "Seviye 1", "L2": "Seviye 2", "RapidFire": "Sv3 · Seri Atış (çift balista)",
          "Sharpshooter": "Sv3 · Keskin Nişancı (gözcü ucu)", "Blast": "Sv3 · Patlama (ateş mangalı)",
          "Frost": "Sv3 · Buz (kristal kümesi)", "ShieldGuard": "Sv3 · Kalkan Muhafız (kalkan duvarı)",
          "AssaultGuard": "Sv3 · Hücum Muhafız (çapraz mızrak)"}
MOCK_SLOTS = {
    720: [(230, 304), (496, 243), (367, 453), (519, 469), (738, 469), (842, 313), (922, 373), (1058, 260)],
    960: [(230, 424), (496, 363), (367, 573), (519, 589), (738, 589), (842, 433), (922, 493), (1058, 380)],
}
MOCK_TOWERS = ["archer_L1", "mage_L1", "barracks_L1", "archer_L2", "barracks_ShieldGuard", "mage_Frost",
               "archer_RapidFire", "mage_L2"]


def a2():
    src = OUT / "a2"
    tile, pad, head = 340, 24, 112
    width = pad * 2 + 4 * tile + 3 * 12
    height = head + 3 * (tile + 46) + 30
    sheet = Image.new("RGB", (width, height), BG)
    d = ImageDraw.Draw(sheet)
    d.text((pad, 22), "KULELER V3  ·  TAM SET (yön 1: yumuşak stilize)", font=font(32, True), fill=GOLD)
    d.text((pad, 66), "L1 › L2 (ek kat + süs) › iki Sv3 uzmanlaşma · oyun kamerası 43° · tek ortak malzeme + renk "
           "atlası, her kule tek mesh", font=font(17), fill=MUTED)
    for r, (kind, label, variants) in enumerate(KINDS):
        y = head + r * (tile + 46)
        d.text((pad, y), label, font=font(20, True), fill=INK)
        for c, variant in enumerate(variants):
            x = pad + c * (tile + 12)
            im = Image.open(src / f"{kind}_{variant}.png").convert("RGB").resize((tile, tile), Image.LANCZOS)
            sheet.paste(im, (x, y + 30))
            d.text((x + 10, y + 30 + tile - 26), LABELS[variant], font=font(14, True), fill=INK)
    sheet.save(OUT / "towers-v3-sheet.png")

    big = 560
    close = Image.new("RGB", (pad * 2 + 3 * big + 24, big + 100), BG)
    d = ImageDraw.Draw(close)
    d.text((pad, 20), "KULELER V3  ·  YAKIN PLAN (Seviye 2)", font=font(28, True), fill=GOLD)
    for c, (kind, label, _) in enumerate(KINDS):
        x = pad + c * (big + 12)
        close.paste(Image.open(src / f"{kind}_L2.png").convert("RGB").resize((big, big), Image.LANCZOS), (x, 70))
        d.text((x + 12, 70 + big - 32), label, font=font(18, True), fill=INK)
    close.save(OUT / "towers-v3-closeup.png")

    panels = []
    for h in (720, 960):
        battle = Image.open(ROOT / "Builds" / f"step6-after-chapter1-prep-1280x{h}.png").convert("RGBA")
        ppu = h / 17.8
        for (sx, sy), tag in sorted(zip(MOCK_SLOTS[h], MOCK_TOWERS), key=lambda item: item[0][1]):
            tower = Image.open(src / f"{tag}_game{h}.png").convert("RGBA")
            oy = 0.2 * 0.73 * ppu
            battle.alpha_composite(tower, (int(sx - tower.width / 2), int(sy + oy - tower.height / 2)))
        panels.append(battle.convert("RGB"))
    mock = Image.new("RGB", (1280 * 2 + pad * 3, 960 + 150), BG)
    d = ImageDraw.Draw(mock)
    d.text((pad, 18), "SAVAŞ EKRANI MOCK'U  ·  kuleler gerçek ekran görüntüsündeki yuvalara 1:1 piksel ölçekte",
           font=font(28, True), fill=GOLD)
    d.text((pad, 60), "Sol 1280×720 · sağ 1280×960 · yuvalar: Okçu L1, Büyücü L1, Kışla L1, Okçu L2, Kalkan Muhafız, "
           "Buz, Seri Atış, Büyücü L2", font=font(17), fill=MUTED)
    mock.paste(panels[0], (pad, 100))
    mock.paste(panels[1], (pad * 2 + 1280, 100))
    gy = 100 + 720 + 40
    d.text((pad, gy), "64 px gri tonlama (siluet/değer kontrolü)", font=font(18, True), fill=INK)
    x = pad
    for kind, _, variants in KINDS:
        for variant in variants:
            g = Image.open(src / f"{kind}_{variant}_game960.png").convert("RGBA")
            g = g.crop(g.getbbox())
            scale = 64 / g.height
            g = g.resize((max(1, int(g.width * scale)), 64), Image.LANCZOS)
            back = Image.new("RGBA", (g.width + 12, 76), (22, 34, 44, 255))
            back.alpha_composite(g, (6, 6))
            mock.paste(back.convert("L").convert("RGB"), (x, gy + 34))
            x += back.width + 8
    mock.save(OUT / "in-battle-mock.png")
    print(OUT / "towers-v3-sheet.png", OUT / "towers-v3-closeup.png", OUT / "in-battle-mock.png")


if __name__ == "__main__":
    {"a1": a1, "a2": a2}[sys.argv[1] if len(sys.argv) > 1 else "a1"]()
