# Faz 1 — UI tasarım sistemi

Tarih: 2026-10-12  
Worktree: `/Users/emreaydin/orca/workspaces/AllinDefense/ui-faz0`  
Unity: `6000.4.1f1`  
Durum: Uygulama ve asset üretimi tamam; EditMode 154/154, PlayMode 117/117 başarılı; toplam 271/271. Commit/push yapılmadı.

## Kapsam ve kaynak

`UiTokens` Temiz Fantastik tasarımının tek renk/süre kaynağıdır. Referanslar bu worktree'de bulunmadığı için main worktree'deki `UI_MODERNIZATION_PLAN_2026-10-12.md`, `mockups/tokens.css` ve A/C/B PNG'leri okundu; kopyalanmadı. UGUI, mevcut nesne/test adları ve Faz 0 `FrontendCopy` TR/EN çiftleri korundu. Harita/oyun mantığına dokunulmadı. Koordinatör HUD controller ve frontend theme dosyalarını yalnız renk/süre bağlantısı için sahiplik kapsamına ekledi.

## Token tablosu

| Token / rol | Değer | Kullanım |
|---|---|---|
| `Night` / `NightDeep` | `#061020` / `#02050A` | Lacivert arka plan |
| `Surface` / `SurfaceRaised` | `#091222` / `#080F1C` | Cam tabanı |
| `Glass` / `GlassStrong` | Surface × %68 / SurfaceRaised × %90 alfa | Panel yüzeyi |
| `Gold` / `GoldHigh` / `GoldLow` | `#F2B84B` / `#FFD98A` / `#D4922C` | Ana vurgu ve gradient |
| `GoldText` | `#1D1305` | Altın CTA üzeri metin |
| `ButtonBlue` | RGB `(0,031; 0,216; 0,522)` | Mevcut koyu menü düğmesi; açık metinle ≥4,5:1 |
| `Moon` / `Blue` | `#9BCBFF` / `#2E75C9` | Ay mavisi vurgu |
| `Danger` / `Success` / `Violet` | `#EF6A4F` / `#49E07B` / `#A77BFF` | Tehlike, hazır, büyü |
| `TextPrimary` / `TextSecondary` | `#F4FAFF` / `#A5BBCC` | Birincil/ikincil metin |
| `Faint` | `#6F8599` | Yalnız dekoratif; zorunlu metinde kullanılmaz |
| `Hairline` / `HairlineStrong` / `HairlineGold` | Moon %16 / Moon %28 / Gold %50 | Kontur |
| `GlassHighlight` | Beyaz %7 | Üst highlight |
| `SpaceUnit`, `Space1/2/3/4/6/8` | 8; 8/16/24/32/48/64 pt | Canvas referans biriminde boşluk |
| `RadiusSmall/Medium/Card/Pill` | 10/14/20/64 pt | Köşe yarıçapları |
| `Shadow`, `ShadowOffset`, `ShadowSoftness` | Siyah %38; `(0,-8)`; 24 pt | Yumuşak alt gölge |
| `GoldShadow` | Altın %35 | Altın vurgu gölgesi |
| `TypeCaption/Label/Body/Subheading/Heading/Display` | 20/24/28/36/48/64 | Mevcut Canvas tipografi ölçeği |
| `WeightRegular/SemiBold/Bold` | 400/600/700 | Font rol kimlikleri |
| `Fade/Slide/ReducedMotion/ButtonPress/ButtonColorDuration` | 0,18/0,22/0,10/0,09/0,08 s | Hareket süreleri |
| `PressScale` / `FocusScale` | 0,975 / 1,025 | Düğme ölçeği |
| `EaseOut` / `EaseInOut` | Kübik ease-out / smoothstep | Her erişimde yeni curve; ortak token değiştirilemez |
| `EvaluateEaseOut` | `1-(1-t)^3` | Allocation gerektirmeyen hareket eğrisi |

Eski ekranların diğer yüzey, scrim, kontur, cooldown ve durum renkleri `UiTokens` içindeki anlamlı adlara taşındı (`ProfileSurface`, `SettingsScrim`, `HudButtonReady`, `HudFrame`, `CooldownStart/End`, vb.). Dinamik vurgular `WithAlpha`, `TintSurface`, `PreviewSurface` yardımcılarıyla oluşturulur. Görsel düzen korunur; HUD/metin ana vurguları ortak palete geçti. `HudButtonPressed` RGB `(0,120; 0,280; 0,400)` olarak koyulaştırıldı; böylece ikincil metin de ≥4,5:1 olur. Menüde altın düğmeler `GoldText` kullanır; parlak `Blue` vurgu olarak kalırken düğme yüzeyi mevcut `ButtonBlue` koyuluğunu korur.

`FrontendPresentationTheme` başlangıçta ve builder sırasında tokenları uygular; eski serialized palet ikinci bir doğruluk kaynağı olarak kalmaz. HUD palette sabitleri ve tüm HUD renk kurucuları tokenlara bağlandı. `FrontendButtonFeedback`, menü reveal ve HUD pause eğrileri/süreleri de ortak kaynağı kullanır. Ekrana özgü ölçüler ve animasyon kapsamı sonraki fazlarda ele alınabilir.

## Font

- Yerel kaynak, koordinatör onayıyla: `/Users/emreaydin/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/libreoffice-headless/libreoffice/LibreOfficeDev.app/Contents/Resources/fonts/truetype/NotoSans-Bold.ttf`.
- Repo kopyası: `Assets/Art/Fonts/NotoSans-Bold.ttf`. Font name tablosu **Noto Sans / Bold**, OS/2 ağırlığı **700**, lisans **SIL OFL 1.1** olarak doğrulandı; mevcut `Assets/ThirdParty/Noto/OFL.txt` lisansı korunur. İnternetten indirme yapılmadı.
- Kaynak cmap kontrolü: `ç ğ ı İ ö ş ü Ç Ğ Ö Ş Ü` tam. Kanıt: `Builds/faz1-font-verification.txt`.
- `UiDesignSystemAssetBuilder` Regular ve Bold TMP atlaslarını ASCII + Türkçe karakterlerle önceden doldurur; mevcut gövde atlasının noktalama/aksanlı karakter kapsamı da Bold yüzüne taşınır. Her iki atlas 122 karakter içerir, özgün 121 karakterin hiçbiri kaybolmadı. Ana Regular/Bold atlasları mevcut proje sözleşmesi uyarınca statiktir; dinamik `NotoSans-ExternalFallback` dış oyuncu adlarını karşılamaya devam eder. Yeni Bold yüzü aynı dış-ad fallback listesini paylaşır; atlas sayfaları assetin subassetleri olarak kaydedilir.
- `NotoSans-DC.asset` weight table 600 ve 700 rolleri gerçek `NotoSans-Bold-DC.asset` yüzüne bağlanır; mevcut `<b>` ve `FontStyles.Bold` metinleri gerçek Bold glifleri kullanır. Bold yüzün ek sentetik ağırlığı sıfırdır.
- **Eksik:** Yerelde gerçek Noto Sans SemiBold yok. Koordinatör kararıyla 600 rolü şimdilik gerçek 700 yüzüne bağlandı; ayrı `semiBoldFont` rolü daha sonra değiştirilebilir. Cinzel SemiBold başlık yüzü korunur.

## Sprite listesi

Tüm PNG'ler `Assets/Art/GeneratedPresentation/UiPrimitives/` içinde 128×128, FullRect, bilinear, mipmapsiz, sıkıştırmasız ve alfa kanallıdır. Sprite listesi ve font rolleri `UiDesignSystemAssets.asset` içinde tutulur; mevcut `Assets/Art/UI/AllinDefenseUI.spriteatlas` genişletilir. Eski on sprite korunur; rotation/tight packing kapalı kalır.

| Sprite | Geometri | 9-slice border (L/B/R/T) |
|---|---|---|
| `Rounded10` | 10 pt yuvarlak köşe | 12/12/12/12 |
| `Rounded14` | 14 pt yuvarlak köşe | 16/16/16/16 |
| `Rounded20` | 20 pt yuvarlak köşe | 22/22/22/22 |
| `Pill64` | Tam yuvarlak | 63/63/63/63 |
| `SoftShadow` | 20 pt köşe + 24 px yumuşak alfa düşüşü | 46/46/46/46 |
| `VerticalGradient` | Üstte %7 beyaz, %45 yükseklikte sıfır alfa | 0/0/0/0 |
| `GoldGradient` | Alt GoldLow → %55 Gold → üst GoldHigh | 0/0/0/0 |
| `FilledCircle` | 62 px yarıçap, antialias | 0/0/0/0 |
| `CooldownRing` | 56 px merkez yarıçap, 12 px kalınlık | 0/0/0/0 |

Beyaz şekiller UI Image tint'iyle renklendirilir. Gölge şekli `UiTokens.Shadow` veya `GoldShadow` ile kullanılabilir. Gradientler `Image.Type.Simple`, daire/halka ise kare RectTransform içinde `Image.Type.Filled / Radial360` için uygundur; ring yeni cooldown bileşeni için hazırdır. Yuvarlak şekiller `Image.Type.Sliced` ile kullanılır; kenarların bozulmaması için minimum boyut iki border toplamını aşmalıdır. Gerçek arka plan blur/grab-pass yoktur.

## Üretim

Unity menüsü: **ALLIN: Defense > Build UI Design System Assets**.

```sh
unity run . --editor-version 6000.4.1f1 --timeout 600 -- \
  -executeMethod AllinDefense.Editor.UiDesignSystemAssetBuilder.Build \
  -logFile Builds/faz1-assets.log
```

Frontend ve tam presentation builder girişleri aynı tasarım sistemi builder'ını çağırır. Faz 0 HUD yeniden üretim metodu ve varsayılan prefab koruma davranışı korunur. Bu faz mevcut HUD prefabını yeniden üretmez; yeni primitive kullanımını Faz 2/3 bileşenleri yapacak.

## Satır içi renkler ve doğrulama

- `rg -n 'new Color(32)?\(' Assets/Scripts/UI --glob '!UiTokens.cs'`: **0**. Gerekçeli istisna yok.
- `DefenderHudController.cs`: açık veya target-typed renk kurucusu **0**. Dinamik vurgular token yardımcılarına bağlıdır.
- Menü tema renk başlangıçları: **0** renk kurucusu. Dünya materyali/ışık renkleri bu fazın kapsamı dışındadır.
- Yeni EditMode testleri: atlasın gerçek character/glyph tablolarında 12 Türkçe glif (Regular/Bold), gerçek 600/700 yüz çözümü, token değerleri/8 pt ızgara/curve izolasyonu, metin kontrastı ≥4,5:1, menü/HUD palet birliği, dokuz sprite/9-slice/atlas üyeliği, piksel geometrisi ve tekrar üretimde PNG/GUID korunması.
- Kontrast sRGB lineerleştirmeyle hesaplanır. Cam kontrastı koyu `Night` backing ve en parlak %7 highlight üstünde ölçülür; rastgele parlak 3B arka plan üzerinde sınırsız bir garanti değildir.
- Unity'nin yerel Roslyn derleyicisi ve mevcut response/reference dosyalarıyla Runtime, Editor ve yeni EditMode kaynakları `/tmp/faz1-compile` çıktısına derlendi: üçü de exit 0. Bu statik kontrol Unity test koşusunun yerine geçmez.
- Asset üretimi: `UiDesignSystemAssetBuilder.Build` exit 0; `Builds/faz1-assets.log`. Statik fontların kaynak referansı ve dinamik dış-ad fallback sözleşmesi korundu. Atlas üyeliği kaynak PNG texture'larıyla tutulur; paketlenmiş geçici texture referansı kaydedilmez.
- EditMode: **154/154 başarılı**, 0 başarısız / 0 atlanan; **11 yeni test vakası**. Sonuç: `Builds/faz1-editmode.xml`.
- PlayMode: **117/117 başarılı**, 0 başarısız / 0 atlanan; 511,84 saniye. Sonuç: `Builds/faz1-playmode.xml`. Toplam **271/271** test yeşil.
- İlk koşuların bulguları düzeltildi: ana fontlar statik bırakıldı, Unity çıkışında glif temizlenmesi engellendi, özgün glif kapsamı korundu, atlas kaynak referansları düzeltildi ve paketleme kontrolü hedef-platform paketlemesinden sonra yapıldı. Nihai EditMode koşusu tüm paketi yeniden çalıştırdı.
- Unity offline lisansı yenilendikten sonra üretim/testlere devam edildi; teslimde lisans engeli kalmadı.
- `ProjectSettings` başlangıç baytlarıyla aynı; Addressables `link.xml` ve metası başlangıç kopyalarından geri yüklendi. ProjectSettings ve Addressables content-state dahil bu alanlarda git farkı yok.
- Faz 0'ın `FrontendCopy` katalogu, ölü dosya silmeleri, HUD yeniden üretim yolu ve testleri korunur. HUD prefabı HEAD baytlarıyla aynı; `AppFlowController.ClosedTest.cs`, `GameLocalization.cs` ve README başlangıç kopyalarıyla birebir aynı.
- `Assets/Scripts/Core/**`, `Assets/Scripts/Data/**`, `DefenderBattlefieldDressing.cs` alanlarında fark yok. `git diff --check` geçti.
- Gerçek SemiBold eksikliği onaylı 600→700 rol bağlantısıyla karşılandı; başka engel veya yarım kalan kabul maddesi yok. Yeni primitive'lerin bileşenlere bağlanması Faz 2/3 işidir.

## Faz 1 değişen dosyalar

- `Assets/Art/Fonts/NotoSans-Bold.ttf`
- `Assets/Art/Fonts/NotoSans-Bold.ttf.meta`
- `Assets/Art/GeneratedPresentation/NotoSans-Bold-DC.asset`
- `Assets/Art/GeneratedPresentation/NotoSans-Bold-DC.asset.meta`
- `Assets/Art/GeneratedPresentation/NotoSans-DC.asset`
- `Assets/Art/GeneratedPresentation/UiDesignSystemAssets.asset`
- `Assets/Art/GeneratedPresentation/UiDesignSystemAssets.asset.meta`
- `Assets/Art/GeneratedPresentation/UiPrimitives.meta`
- `Assets/Art/GeneratedPresentation/UiPrimitives/CooldownRing.png`
- `Assets/Art/GeneratedPresentation/UiPrimitives/CooldownRing.png.meta`
- `Assets/Art/GeneratedPresentation/UiPrimitives/FilledCircle.png`
- `Assets/Art/GeneratedPresentation/UiPrimitives/FilledCircle.png.meta`
- `Assets/Art/GeneratedPresentation/UiPrimitives/GoldGradient.png`
- `Assets/Art/GeneratedPresentation/UiPrimitives/GoldGradient.png.meta`
- `Assets/Art/GeneratedPresentation/UiPrimitives/Pill64.png`
- `Assets/Art/GeneratedPresentation/UiPrimitives/Pill64.png.meta`
- `Assets/Art/GeneratedPresentation/UiPrimitives/Rounded10.png`
- `Assets/Art/GeneratedPresentation/UiPrimitives/Rounded10.png.meta`
- `Assets/Art/GeneratedPresentation/UiPrimitives/Rounded14.png`
- `Assets/Art/GeneratedPresentation/UiPrimitives/Rounded14.png.meta`
- `Assets/Art/GeneratedPresentation/UiPrimitives/Rounded20.png`
- `Assets/Art/GeneratedPresentation/UiPrimitives/Rounded20.png.meta`
- `Assets/Art/GeneratedPresentation/UiPrimitives/SoftShadow.png`
- `Assets/Art/GeneratedPresentation/UiPrimitives/SoftShadow.png.meta`
- `Assets/Art/GeneratedPresentation/UiPrimitives/VerticalGradient.png`
- `Assets/Art/GeneratedPresentation/UiPrimitives/VerticalGradient.png.meta`
- `Assets/Art/UI/AllinDefenseUI.spriteatlas`
- `Assets/Editor/DefenderPresentationAssetBuilder.cs`
- `Assets/Editor/UiDesignSystemAssetBuilder.cs`
- `Assets/Editor/UiDesignSystemAssetBuilder.cs.meta`
- `Assets/Resources/Presentation/FrontendPresentationTheme.asset`
- `Assets/Scripts/Presentation/DefenderHudController.cs`
- `Assets/Scripts/Presentation/FrontendPresentationTheme.cs`
- `Assets/Scripts/UI/AppFlowController.Hero.cs`
- `Assets/Scripts/UI/AppFlowController.Screens.cs`
- `Assets/Scripts/UI/AppFlowController.cs`
- `Assets/Scripts/UI/FrontendButtonFeedback.cs`
- `Assets/Scripts/UI/FrontendLoadingOverlay.cs`
- `Assets/Scripts/UI/FrontendTacticalMapPreview.cs`
- `Assets/Scripts/UI/UiDesignSystemAssets.cs`
- `Assets/Scripts/UI/UiDesignSystemAssets.cs.meta`
- `Assets/Scripts/UI/UiTokens.cs`
- `Assets/Scripts/UI/UiTokens.cs.meta`
- `Assets/Tests/EditMode/UiDesignSystemTests.cs`
- `Assets/Tests/EditMode/UiDesignSystemTests.cs.meta`
- `docs/ui-modernization/FAZ1_DESIGN_SYSTEM_2026-10-12.md`
