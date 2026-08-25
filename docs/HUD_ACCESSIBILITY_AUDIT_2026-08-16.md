## HUD Accessibility Audit — 2026-08-16

Kapsam: `VIS-611` için mevcut production HUD üzerinde küçük metin, kontrast ve touch-target erişilebilirlik denetimi.

Referanslar:

- `Assets/Scripts/Presentation/DefenderHudController.cs`
- `Assets/Scripts/Presentation/DefenderHudResponsiveLayout.cs`
- `Assets/Art/GeneratedPresentation/DefenderHud.prefab`
- `Assets/Tests/EditMode/CoreRulesTests.cs`
- `docs/evidence/runtime/defender-simulator-hud-pause-overlay-pass-landscape-readable.png`

### Bu pass'te yapılan düzeltmeler

- Küçük yardımcı metinler büyütildi:
  - `phaseHintText` `13f → 15f`
  - `previewTitleText` `15f → 16f`
  - `heroText` `20f → 21f`
  - `selectedHintText` `15f → 16f`
  - `resultHintText` `18f → 19f`
  - `pauseOverlayHintText` `18f → 19f`
- Uzun yardımcı metinler için word-wrap/overflow iyileştirildi:
  - `selectedHintText`
  - `resultHintText`
  - `pauseOverlayHintText`
- Pause overlay kartı ve hint alanı genişletildi; açıklama satırı artık kırpılmadan okunuyor.

### Kontrast denetimi

Kodda tanımlı ana foreground/background çiftleri için hesaplanan kontrastlar:

| Ön plan | Arka plan | Oran | Sonuç |
|---|---|---:|---|
| `TextPrimary` | `RailTint` | `17.46:1` | Geçer |
| `TextPrimary` | `PanelTint` | `17.01:1` | Geçer |
| `TextMuted` | `RailTint` | `8.72:1` | Geçer |
| `TextMuted` | `PanelTint` | `8.49:1` | Geçer |
| `GoldAccent` | `RailTint` | `12.64:1` | Geçer |
| `WaveAccent` | `RailTint` | `11.82:1` | Geçer |
| `ReadyAccent` | `RailTint` | `14.33:1` | Geçer |
| `KeepAccent` | `PanelTint` | `6.44:1` | Geçer |

Sonuç: ana HUD bilgi yüzeylerinde kontrast açığı değil, daha çok metin boyutu ve yerleşim problemi vardı.

### Touch-target denetimi

WCAG 2.2 minimum hedefi ve iOS pratik hedefi olarak `44x44pt` alt sınırı baz alındı.

Mevcut ölçüler:

| Kontrol | Ölçü | Kaynak | Sonuç |
|---|---|---|---|
| Bottom bar build/ability/cancel butonları | `~74 px` yükseklik | `DefenderHudResponsiveLayout.SetButtonRange()` | Geçer |
| Pause butonu | `~62 px` yükseklik | `DefenderHudResponsiveLayout.SetRange()` | Geçer |
| Upgrade / Sell | `138x58 px` | `DefenderHud.prefab` | Geçer |
| Restart | `350x86 px` | `DefenderHud.prefab` + runtime resize | Geçer |
| NextWave | `320x70 px` prefab, responsive'de daha geniş | `DefenderHud.prefab` / layout | Geçer |
| Ability | `246x70 px` prefab, responsive'de daha geniş | `DefenderHud.prefab` / layout | Geçer |

Sonuç: kritik etkileşimli alanlarda minimum touch-target eşiğinin altında kalan bir kontrol bulunmadı.

Ek güvence:

- `ProductionHudTouchTargetsStayAboveMinimum` edit-mode testi, production HUD prefab'ındaki ana etkileşimli alanların `44x44` altına düşmesini otomatik olarak yakalar.

### Durum bilgisinin yalnız renge bağlı olmaması

Bu HUD ailesinde durum artık yalnız renkle aktarılmıyor:

- ability state: `HAZIR`, saniye bilgisi, `DURAKLADI`, `DÖNÜYOR`
- pause state: phase badge + button label + merkez overlay
- build state: fiyat / yetersiz kaynak metni + alt durum şeridi
- phase state: `DURUM`, `DALGA ÖNCESİ`, `DURAKLATILDI` gibi ayrı kopya

Bu, `VIS-111` hedefiyle uyumlu ilk güçlü adımdır.

### Runtime kanıt

| Kanıt | Yol | Not |
|---|---|---|
| Pause overlay readability | `docs/evidence/runtime/defender-simulator-hud-pause-overlay-pass-landscape-readable.png` | Uzun yardımcı metin artık kırpılmıyor; paused state birden fazla sinyalle okunuyor |

### Açık kalanlar

- 4:3 ve tablet oranlarında aynı readability kontrolü yapılmadı.
- Victory / defeat ekranları için ayrı accessibility acceptance screenshot'ları eksik.
- Gerçek VoiceOver / assistive-tech akışı doğrulanmadı; bu pass görsel ve touch audit seviyesindedir.
- Final grayscale / color-blind / input regression kapanışı `VIS-813` altında tekrar kontrol edilmeli.

### Karar

`VIS-611` için ilk audit ve düzeltme pass'i tamamlandı. Mevcut HUD artık daha büyük yardımcı metinler, geçerli kontrast ve yeterli touch-target ölçüleriyle daha güvenli bir tabana sahip. Ancak oran bazlı acceptance ve assistive-tech doğrulaması tamamlanmadığı için bu kapanış değil, inceleme aşamasıdır.
