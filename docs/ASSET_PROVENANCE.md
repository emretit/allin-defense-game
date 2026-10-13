# ALLIN: Defense — Varlık Kökeni ve Lisans Kaydı

Son güncelleme: 2026-10-13

Production kataloğu bu kayıtların karşılığını doğrular. Zorunlu font lisansı, kaynak kaydı veya sunum varlığı eksikse iOS/production build başlamaz.

| Varlık | Köken | Lisans / kullanım | Kaynak | Tarih |
|---|---|---|---|---|
| Noto Sans Regular | Noto Fonts | SIL Open Font License 1.1 | https://github.com/notofonts/noto-fonts | 2026-08-16 |
| Noto Serif SemiBold | Noto Fonts | SIL Open Font License 1.1 | https://github.com/notofonts/noto-fonts | 2026-08-16 |
| HUD panel dokusu | OpenAI image generation; uygulama ikonu görsel referans alındı | Bu proje için üretilen özgün çıktı | Bu proje oturumu | 2026-08-16 |
| HUD ikon atlası | OpenAI image generation; HUD paneli görsel referans alındı | Bu proje için üretilen özgün çıktı | Bu proje oturumu | 2026-08-16 |
| Art quality hedef karesi | OpenAI image generation; yalnız mevcut ALLIN: Defense ekran düzeni referans alındı | Bu proje için üretilen özgün iç sanat referansı | `docs/art/defender-quality-target-v1.png` | 2026-08-16 |
| Çevre, aktör ve karakol modelleri | `tools/art_pipeline/generate_lowpoly_assets.py` ile Blender'da üretildi | ALLIN: Defense özgün varlığı | `ArtSource/Blender` | 2026-08-16 |
| SFX | Kenney Interface Sounds, Impact Sounds ve RPG Audio | Creative Commons CC0 1.0 | Kaynaklar `Assets/ThirdParty/Audio/CC0Sources/SOURCES.md` içinde | 2026-08-16 |
| Müzik | OpenGameArt üzerinde yayımlanan altı eser | Creative Commons CC0 1.0 | Eser adı, sanatçı ve bağlantılar `Assets/ThirdParty/Audio/CC0Sources/SOURCES.md` içinde | 2026-08-16 |

Noto lisans metni `Assets/ThirdParty/Noto/OFL.txt`, CC0 1.0 hukuki metni `Assets/ThirdParty/Audio/CC0Sources/CC0-1.0-Legal-Code.txt` altında paketlenir. İndirilen özgün arşivler/dosyalar, eser adları, sanatçılar, kaynak bağlantıları ve indirme tarihi repoda korunur.

## Üretim çıktıları

- Düzenlenebilir Blender kaynakları: `ArtSource/Blender`
- Unity FBX çıktıları: `Assets/Art/Models/Generated`
- Ortak URP materyalleri ve HUD prefabı: `Assets/Art/GeneratedPresentation`
- Runtime SFX ve müzik türevleri: `Assets/Audio/Presentation`
- CC0 kaynak arşivleri ve lisans kayıtları: `Assets/ThirdParty/Audio/CC0Sources`
- Noto font dosyaları: `Assets/Art/Fonts`
- Üretim ve CC0 içe aktarma betikleri: `tools/art_pipeline`
- Art quality kontratı ve özgün hedef karesi: `docs/ART_QUALITY_CONTRACT.md`, `docs/art`

## Savaş halka menüsü ikonları — 2026-10-13

| İkon | Kaynak | Tarih | Dosya | Prompt |
|---|---|---|---|---|
| Upgrade | Codex image_generation | 2026-10-13 | `Assets/Art/UI/Radial/IconUpgrade.png` | Use case: stylized-concept. Produce one 512x512 mobile fantasy tower-defense Upgrade icon. Style reference is the existing AllinDefense archer emblem: hand-painted slightly 3D faceted metal with bold silhouette, steel and gold medallion rim, dark navy circular inner background, soft light from above, gold and ice-blue accents. Subject: a single thick ascending golden arrow with two golden chevrons beneath. Front view, centered circular medallion fills 90% of square. Readable immediately at 56 points, simple large subject, no thin line art. NO text, letters, digits, watermarks. Outside the medallion completely transparent. Match attached style reference but do not redraw its bow. |
| Sell | Codex image_generation | 2026-10-13 | `Assets/Art/UI/Radial/IconSell.png` | Use case: stylized-concept. Create one 512x512 mobile fantasy tower-defense Sell game icon. Hand-painted slightly 3D faceted metal, bold readable silhouette. Steel-gold circular medallion rim, dark navy inner disc, gold and ice-blue highlights, soft light from above. Match AllinDefense fantasy tower icons. Subject: a large golden coin stack with one small leather coin purse, strong gold highlights. Front view centered medallion filling 90% of square. Large simple shapes readable at 56pt. NO text letters digits watermarks; no thin white line glyphs. Outside the medallion transparent. |
| RapidFire | Codex image_generation | 2026-10-13 | `Assets/Art/UI/Radial/IconRapidFire.png` | Use case: stylized-concept. Create one 512x512 mobile fantasy tower-defense RapidFire game icon. Hand-painted slightly 3D faceted metal, bold readable silhouette. Steel-gold circular medallion rim, dark navy inner disc, gold and ice-blue highlights, soft light from above. Match AllinDefense fantasy tower icons. Subject: three large arrows descending as an arrow rain, orange-gold shafts and ice-blue arrowheads. Front view centered medallion filling 90% of square. Large simple shapes readable at 56pt. NO text letters digits watermarks; no thin white line glyphs. Outside the medallion transparent. |
| Sharpshooter | Codex image_generation | 2026-10-13 | `Assets/Art/UI/Radial/IconSharpshooter.png` | Use case: stylized-concept. Create one 512x512 mobile fantasy tower-defense Sharpshooter game icon. Hand-painted slightly 3D faceted metal, bold readable silhouette. Steel-gold circular medallion rim, dark navy inner disc, gold and ice-blue highlights, soft light from above. Match AllinDefense fantasy tower icons. Subject: a thick golden targeting crosshair behind one large diagonal arrow, warm orange-gold accents. Front view centered medallion filling 90% of square. Large simple shapes readable at 56pt. NO text letters digits watermarks; no thin white line glyphs. Outside the medallion transparent. |
| ShieldGuard | Codex image_generation | 2026-10-13 | `Assets/Art/UI/Radial/IconShieldGuard.png` | Use case: stylized-concept. Create one 512x512 mobile fantasy tower-defense ShieldGuard game icon. Hand-painted slightly 3D faceted metal, bold readable silhouette. Steel-gold circular medallion rim, dark navy inner disc, gold and ice-blue highlights, soft light from above. Match AllinDefense fantasy tower icons. Subject: one large steel shield with red central inset and small gold boss. Front view centered medallion filling 90% of square. Large simple shapes readable at 56pt. NO text letters digits watermarks; no thin white line glyphs. Outside the medallion transparent. |
| AssaultGuard | Codex image_generation | 2026-10-13 | `Assets/Art/UI/Radial/IconAssaultGuard.png` | Use case: stylized-concept. Create one 512x512 mobile fantasy tower-defense AssaultGuard game icon. Hand-painted slightly 3D faceted metal, bold readable silhouette. Steel-gold circular medallion rim, dark navy inner disc, gold and ice-blue highlights, soft light from above. Match AllinDefense fantasy tower icons. Subject: two broad crossed steel swords with red grips and gold guards. Front view centered medallion filling 90% of square. Large simple shapes readable at 56pt. NO text letters digits watermarks; no thin white line glyphs. Outside the medallion transparent. |
| Blast | Codex image_generation | 2026-10-13 | `Assets/Art/UI/Radial/IconBlast.png` | Use case: stylized-concept. Create one 512x512 mobile fantasy tower-defense Blast game icon. Hand-painted slightly 3D faceted metal, bold readable silhouette. Steel-gold circular medallion rim, dark navy inner disc, gold and ice-blue highlights, soft light from above. Match AllinDefense fantasy tower icons. Subject: one large exploding purple magic orb with bold violet flame lobes and ice-blue sparks. Front view centered medallion filling 90% of square. Large simple shapes readable at 56pt. NO text letters digits watermarks; no thin white line glyphs. Outside the medallion transparent. |
| Frost | Codex image_generation | 2026-10-13 | `Assets/Art/UI/Radial/IconFrost.png` | Use case: stylized-concept. Create one 512x512 mobile fantasy tower-defense Frost game icon. Hand-painted slightly 3D faceted metal, bold readable silhouette. Steel-gold circular medallion rim, dark navy inner disc, gold and ice-blue highlights, soft light from above. Match AllinDefense fantasy tower icons. Subject: one large faceted ice crystal with purple-blue facets and a frosty cyan glow. Front view centered medallion filling 90% of square. Large simple shapes readable at 56pt. NO text letters digits watermarks; no thin white line glyphs. Outside the medallion transparent. |
