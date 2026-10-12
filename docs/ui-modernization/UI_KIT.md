# Adım 3 — ortak UGUI bileşen kiti

`Assets/Scripts/UI/Kit/UiKit.cs` tek fabrika kaynağıdır; namespace `AllinDefense.UI.Kit`, UnityEditor bağımlılığı yoktur. Runtime menüler `theme.designSystem`, Editor HUD builder ise aynı `UiDesignSystemAssets` asset'i ile `new UiKit(assets)` oluşturabilir. Bu adım mevcut ekranlara bağlanmaz; sahne ve HUD prefab'ı değiştirilmez.

## Kullanım sözleşmesi

```csharp
var kit = new UiKit(designSystem);
var action = kit.Button(parent, "Hud.Upgrade", FrontendCopy.UpgradeStat,
    UiButtonVariant.Gold, designSystem.iconUpgrade, price: 120);
action.onClick.AddListener(OnUpgrade);
action.GetComponent<UiVisualState>().SetState(UiState.Disabled);

var cooldown = kit.RadialCooldown(parent, "Hud.Ability", designSystem.iconShield);
cooldown.SetCooldown(remainingSeconds, totalSeconds);
```

- Her fabrika `parent` ve kalıcı `id` alır. Alt kimlikler bu kimlikten türetilir. `UiId` nesne adlarından bağımsız test sorguları için kullanılabilir.
- Üst yerleşim çağırana aittir. Bileşenler kendi içeriklerini LayoutGroup/LayoutElement ile ölçer; metin minimum satır yüksekliğinin altına sıkışmaz. `UiKit.Layout`, `Size`, `Node`, `Stretch` yardımcıları aynı kuralları Editor'de de kullanır.
- `UiPointSize`, `UiMetrics` ile gerçek mantıksal dokunma boyutunu hesaplar: en az 44 pt, Gold CTA 52 pt, yetenek/halkalı seçenek 72 pt. `UiProportionalIcon` dairesel hedefin %56'sını kullanır ve Canvas ölçeğiyle birlikte hesaplanır.
- `UiVisualState`: Normal / Pressed / Disabled / Selected / Ready. Basılı durum koyulaşır ve 0,975 ölçeğe iner; pasifte ikon gri, fiyat kırmızıdır. Seçim altın kontur/parıltıdır. Hazır durum dairesel hedefte altın halka, dikdörtgen hedefte dış kontur/parıltıdır ve girişte en fazla iki nabız oynar; tekrarlı Ready çağrıları nabzı yeniden başlatmaz.
- Fabrikalara yerelleştirilebilir metin `FrontendCopy` kimliği olarak verilir. `UiKitText` TR/EN çiftini serialize eder ve dil değişimini dinler. Dinamik sayı alanları bağımsızdır.
- Fabrikaya verilen C# callback'ler runtime abonelikleridir; Editor'de kaydedilen prefab'larda oyuna ait callback'ler ekran/controller tarafından runtime'da bağlanmalıdır. Switch, SegmentedControl, RadialMenu ve Overlay kendi iç bağlantılarını yüklenince yeniden kurar.

## Bileşenler ve olaylar

| Fabrika / sınıf | Davranış |
| --- | --- |
| Surface / Card | 10/14/20 yarıçap, koyu cam, atlas highlight gradient, mavi kontur, gölge |
| Button / IconButton | Gold / Glass / Danger; opsiyonel ikon, fiyat; circle veya pill |
| StatPill | Tabular sayı, `SetValue`, `Changed(delta)`, +N değişim alanı |
| Chip | İçeriğe göre ölçülen pill etiket |
| Switch | Açık/Kapalı metni ve check/X simgesi; `SetValue`, `Changed(bool)` |
| Slider | İkon, dolgu, handle; standart UGUI Slider / onValueChanged |
| SegmentedControl | Tek seçim; `Select`, `Changed(index)`; pasif segment seçilemez |
| ProgressSegments | `SetProgress(completedSegments)`; örneğin 2,5 → iki tam + yarım segment |
| RadialCooldown | `SetCooldown(remaining, duration)`; radial360 dolgu, ayrı saniye/HAZIR rozeti; oyun saatini controller sağlar |
| RadialMenu | `Open(screenPoint)`, `OpenWorld(worldPoint, camera)`, `OpenLocal` ve `SetAvailable` |
| Modal / BottomSheet | `Body` içerik alanı; `Show`, `Hide`; scrim, kapatma, giriş/çıkış; body'ye aynı kit ile bileşen eklenir |
| StarRow | Üç yıldız, `SetStars(0..3)`; sıralı dolum |

Halka menüde ilk seçenek dokunuşu `Previewed(option)` gönderir. Başka bir seçeneğe dokunmak önizlemeyi değiştirir; aynı seçeneğe ikinci dokunuş `Confirmed(option)` gönderir ve kapatır. Boş alana dokunma `Closed` ile kapatır. Menzil/bilgi sunumu `Previewed` olayından, gerçek inşa/ödeme `Confirmed` olayından bağlanmalıdır; menü kendisi oyun verisini değiştirmez. Güvenli alan kenarında yarım yay kullanılır; yay sığmazsa hedefler küçülmeden satırlara sıkıştırılır. Tüm hedeflerin fiziksel olarak sığmadığı alana hata verilir; çağıran seçenek sayısını/alanı buna göre sağlamalıdır.

`UiMotion` unscaled time kullanır. Fade, Scale, Slide, Punch ve Count bir `Handle` döndürür; `Cancel()` sonraki güncellemeleri ve tamamlanma callback'ini durdurur. Host kapanınca hareketler iptal edilir. Azaltılmış hareket açıkken veya EditMode'da nihai durum aynı çağrıda uygulanır; çalışma sırasında tercih açılırsa sonraki frame'de tamamlanır.

## İkonlar ve vitrin

`UiDesignSystemAssetBuilder.BuildSystemIcons` 15 beyaz ince çizgi sistem ikonu üretir: düşman, pause, play, hız, ayar, kapat, geri, harita, kilit, dolu/boş yıldız, check, kalkan, yükselt, sat. Kaynakları `Assets/Art/GeneratedPresentation/UiIcons`, referansları `UiDesignSystemAssets`, hedef atlası mevcut `AllinDefenseUI.spriteatlas`tır. Altın/kale/dalga/kuleler mevcut boyalı atlas kaynaklarından kalır.

`AllinDefense.Editor.UiKitShowcaseBuilder.Render` iki gerçek Unity RenderTexture çıktısı üretir:

- `Builds/ui-kit-showcase.png` — 2622×1206
- `Builds/ui-kit-showcase-portrait.png` — 1206×2622

Üretici geçici preview scene kullanır; boş/kırpılmış TMP metin geometrisini hata kabul eder ve açık sahneyi kaydetmez. Preview kameranın izolasyonu Unity'nin [Camera.scene](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Camera-scene.html) API'siyle sağlanır.

## Doğrulama

- EditMode: **202/202**, `Builds/adim3-edit.xml` (174 baseline + 28 yeni senaryo).
- PlayMode: son kodun tam koşusu **128/128** (124 baseline + 4 hareket senaryosu), `Builds/adim3-play.xml`; 2026-10-12 14:20:49–14:29:22 UTC. İlk tam koşu da 128/128 geçti (`Builds/adim3-play-first.xml`).
- iPhone 17 Pro simülatör build: **başarılı**; kabul `Builds/FrontendExperience/iOS/report.json`: **status: passed**, **errors: []**; 19 ekran görüntüsü üretildi. Raporda mevcut HUD İngilizce metinleri için 6 `knownIssues` taşma kaydı var (BuildChoices ve SelectedPanel açıklamaları); kit bu ekranlara uygulanmadığı için Adım 4–5 kapsamına kalır.
- Vitrin: son koddan iki yön üretildi ve görsel olarak incelendi; her görünür TMP metninin geometrisi üreticide doğrulandı.
- `git diff --check`: temiz; yeni C# dosyalarında da sondaki boşluk taraması temiz ve `.meta` dosyaları mevcut.
- Build kaynaklı ProjectSettings, Addressables content-state ve geçici font materyali farkları geri alındı; `link.xml` ve meta dosyası her Unity batch koşusu sonrasında restore edildi.
- Mevcut AppFlowController, DefenderHudController, DefenderPresentationAssetBuilder, sahne ve prefab dosyalarında diff yok; commit/push yapılmadı.

Yeni testler durum geçişleri, minimum pt, gövde kontrastı, fiyat/metin geometrisi, ikon oranı, rozet-halka ayrılığı, sayısal delta, switch, slider, segmentler, cooldown dolgu, radial güvenli alan/K3, overlay, yıldız, reduced-motion ve beyaz atlas ikonlarını kapsar. PlayMode ekleri duraklatılmış oyun saatinde hareket, iptal, çalışma sırasında reduced-motion ve sınırlı hazır nabzını sınar.

## Adım 4a entegrasyon ekleri

- `UiStatPill.SetValue(value, animate, showDelta)` farkı 0,4 saniye gösterir; aynı değeri tekrar vermek farkın süresini bozmaz. İlk bağlama ve can sayısı için `showDelta: false` kullanılabilir.
- `UiOverlay.DismissOnScrim = false` zorunlu eylem gerektiren pause/onay modallarının boşluğa dokunarak gizlenmesini önler. Dünya kilidini çağıran controller yönetir.
- `iconEnemy` atlas içindeki 15. sistem ikonudur; savaşın sayısal sıradaki-dalga çipinde kullanılır.
