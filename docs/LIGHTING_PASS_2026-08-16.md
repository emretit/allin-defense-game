# VIS-412 — Lighting Pass

Tarih: 2026-08-16
TODO: VIS-412
Durum: Dokümantasyon hazır; mevcut implementasyon ile eşleştirme beklemede.

## Amaç

Gece atmosferinde tek ana key ışık ve sınırlı fill/spot ışıklarıyla okunabilirlik korumak;
Low/Medium profillerde shadow-casting point light ve gereksiz ek gölge yükünü kapatmak.

## Hedef ışık düzeni

### 1) Key light (obligatory)

- Tip: `Directional Light`
- Renk: `%Dawn Blue` (koyu mavi)
- Position/rotation: `sun` +45° pitch, batıdan gelen yatay açı (mevcut `DefenderSceneLook.ConfigureSun()`).
- Hard-edged değil, soft shadow sadece High/kanıtlanır durumda.

### 2) Fill lights (minimum 2)

- İsim: `Moon Fill`, `Gate Ember Fill`
- Tip: `Point Light`
- Point light shadow: `None`
- Görev: sadece ton ve kontrast destek; actor veya yol merkezini maskelemeden destekleyici aydınlatma.
- Aralık: sadece map arka planı/yardımcı siluet odaklı.

### 3) Atmosphere/volumetric destek

- Dokunuşlu ambient band/nebula/fog varlıkları material renk eğrisinden desteklenecek;
- yeni `Light` eklenmeyecek veya shadow cast yükü eklenmeyecek.

## Kalite kapıları (özdeş)

| Profil | Ana gölge | Additional point shadow | Ambient/volumetric | Not |
|---|---|---|---|---|
| Low | Kapalı veya çok kısa mesafe | Kapalı | Düşük | `Atmosphere_DustVeil` kapatılabilir |
| Medium | Kapalı/limit | Kapalı | Orta | `FogRibbon` düşük alfa |
| High | Tek kademeli, kısa/kapalı fallback koşulu | Kapalı (varsayılan) | Aktif | Ana ışık shadow kalıntısı test edilir |

## Mevcut durum (Unity implementasyonu ile karşılaştırma)

- `Assets/Scripts/Presentation/DefenderSceneLook.cs` içinde ana anahtar ışık ve iki fill nokta ışığı kurulu:
  - `ConfigureSun()` — yönlendirilmiş main ışık + kaliteye göre soft/kapalı gölge geçişi.
  - `EnsureFillLight()` — `Moon Fill`, `Gate Ember Fill`.
- Son 5.4 geçişinde kalite-tabanlı profil kuralı eklendi:
  - Low/Medium kalite adlarında ana anahtar ışık gölgesi kapatılıyor.
  - Low kalite modunda fill ışık yoğunluğu/menzili düşürülüyor.
- Bu pass’te hedefimiz bu düzenin `VIS-411` atmosphere ve profil değişimi ile eşlenmesi.

## Kabul kriteri (VIS-412)

- Low/Medium profilinde point light shadow tamamen kapatılacak veya prefab kullanılmayacak.
- Ana ışık/karanlık kontrast, actorlar ile yol zemini arasında ayrımı koruyacak.
- HUD ve battlefield ölçek davranışı, kamera oranı/viewport ile bozulmayacak.
- Görsel/performans kabul metrikleri: `VIS-802`, `VIS-803`, `VIS-807`.

## Kanıt

| Kanıt | Yol | Açıklama |
|---|---|---|
| Profil test | `Assets/Tests/PlayMode/GreyboxSceneTests.cs` (`SceneLookRespectsQualityLightingProfile`) | `low/medium/high` kalite adlarında sun ve fill ışık davranışını doğrulayan PlayMode test |
| Lighting script | `Assets/Scripts/Presentation/DefenderSceneLook.cs` | Ana ışık ve fill ışıkları |
| Doku | `docs/VISUAL_ART_BIBLE_2026-08-16.md` | Mevcut gece kontrast ve URP hedef değerleri |
| TODO bağlama | `docs/VISUAL_DEVELOPMENT_TODO.md` | VIS-412 için durum ve kapılar |

## Yeniden üretim

```bash
# Mevcut sahne konfigürasyonunda ışığı doğrula
git grep -n "ConfigureSun\|EnsureFillLight" Assets/Scripts/Presentation/DefenderSceneLook.cs

# Görsel kalite için capture, oran ve profil testleri (Unity Editor)
# - 16:9, 20:9, 4:3 screenshot seti
# - Low/Medium/High kalite karşılaştırması
# - iPhone 13 / iPhone 17 Pro profili
```
