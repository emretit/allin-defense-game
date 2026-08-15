# Defender Chronicles — Varlık Kökeni ve Lisans Kaydı

Son güncelleme: 2026-08-16

Production kataloğu bu kayıtların karşılığını doğrular. Zorunlu font lisansı, kaynak kaydı veya sunum varlığı eksikse iOS/production build başlamaz.

| Varlık | Köken | Lisans / kullanım | Kaynak | Tarih |
|---|---|---|---|---|
| Noto Sans Regular | Noto Fonts | SIL Open Font License 1.1 | https://github.com/notofonts/noto-fonts | 2026-08-16 |
| Noto Serif SemiBold | Noto Fonts | SIL Open Font License 1.1 | https://github.com/notofonts/noto-fonts | 2026-08-16 |
| HUD panel dokusu | OpenAI image generation; uygulama ikonu görsel referans alındı | Bu proje için üretilen özgün çıktı | Bu proje oturumu | 2026-08-16 |
| Çevre, aktör ve karakol modelleri | `tools/art_pipeline/generate_lowpoly_assets.py` ile Blender'da üretildi | Defender Chronicles özgün varlığı | `ArtSource/Blender` | 2026-08-16 |
| SFX ve müzik döngüleri | `tools/art_pipeline/generate_audio.py` ile sentezlendi | Defender Chronicles özgün varlığı | `Assets/Audio/Presentation` | 2026-08-16 |

Noto lisans metni `Assets/ThirdParty/Noto/OFL.txt` altında paketlenir. Bu teslimde dışarıdan alınmış CC0 ses kullanılmamıştır; dolayısıyla kaydı tutulması gereken üçüncü taraf ses bağlantısı yoktur.

## Üretim çıktıları

- Düzenlenebilir Blender kaynakları: `ArtSource/Blender`
- Unity FBX çıktıları: `Assets/Art/Models/Generated`
- Ortak URP materyalleri ve HUD prefabı: `Assets/Art/GeneratedPresentation`
- SFX ve müzik: `Assets/Audio/Presentation`
- Noto font dosyaları: `Assets/Art/Fonts`
- Üretim betikleri: `tools/art_pipeline`
