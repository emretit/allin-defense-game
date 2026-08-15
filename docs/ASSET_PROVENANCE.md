# Defender Chronicles — Varlık Kökeni ve Lisans Kaydı

Son güncelleme: 2026-08-16

Production kataloğu bu kayıtların karşılığını doğrular. Zorunlu font lisansı, kaynak kaydı veya sunum varlığı eksikse iOS/production build başlamaz.

| Varlık | Köken | Lisans / kullanım | Kaynak | Tarih |
|---|---|---|---|---|
| Noto Sans Regular | Noto Fonts | SIL Open Font License 1.1 | https://github.com/notofonts/noto-fonts | 2026-08-16 |
| Noto Serif SemiBold | Noto Fonts | SIL Open Font License 1.1 | https://github.com/notofonts/noto-fonts | 2026-08-16 |
| HUD panel dokusu | OpenAI image generation; uygulama ikonu görsel referans alındı | Bu proje için üretilen özgün çıktı | Bu proje oturumu | 2026-08-16 |
| HUD ikon atlası | OpenAI image generation; HUD paneli görsel referans alındı | Bu proje için üretilen özgün çıktı | Bu proje oturumu | 2026-08-16 |
| Çevre, aktör ve karakol modelleri | `tools/art_pipeline/generate_lowpoly_assets.py` ile Blender'da üretildi | Defender Chronicles özgün varlığı | `ArtSource/Blender` | 2026-08-16 |
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
