## HUD Overlay State Pass — 2026-08-16

Kapsam: `VIS-610` için pause, victory ve defeat ekranlarını ortak bir fantasy HUD kart ailesine yaklaştıran ilk overlay pass.

### Bu pass'te yapılanlar

- `Assets/Scripts/Presentation/DefenderHudController.cs`
  - Pause state için battlefield'ı tamamen kapatmayan, merkezde kartlaşmış bir `PauseOverlay` eklendi.
  - Victory / defeat ekranı için `ResultPanel` içine ortak kart yüzeyi, badge ve hint satırı eklendi.
  - Pause button, phase badge ve ability state aynı paused diline birlikte düşecek şekilde senkronlandı.
- `Assets/Scripts/Core/GameSessionController.cs`
  - `DC_PAUSE_CAPTURE=1` akışı eklendi; bu akış sahne açıldıktan sonra oyunu otomatik pause'a alıp overlay kanıtının deterministik alınmasını sağlar.

### Kanıt

| Kanıt | Yol | Not |
|---|---|---|
| Pause overlay pass | `docs/evidence/runtime/defender-simulator-hud-pause-overlay-pass-landscape-readable.png` | Ortak kart diliyle merkez pause overlay |
| Pause overlay raw | `docs/evidence/runtime/defender-simulator-hud-pause-overlay-pass-raw.png` | Simulator ham framebuffer çıktısı |

### Gözlem

Pause capture'ında:

- üst bardaki pause button `DEVAM` state'ine geçiyor,
- phase badge `DURAKLATILDI` gösteriyor,
- ability button `BEKLE / DURAKLADI` diline düşüyor,
- merkez overlay battlefield görünürlüğünü korurken pause state'ini netleştiriyor.

### Açık kalanlar

- Victory ve defeat için ayrı acceptance screenshot'ları henüz alınmadı.
- Pause overlay hint satırı dar oranlarda tekrar ölçülmeli.
- Final alpha, blur ve input/accessibility kapanışı açık.

### Karar

Bu pass `VIS-610` için işlevsel ilk milestone'dur. Pause state artık sadece üst button etiketiyle değil, merkez overlay ve üst bar state ailesiyle birlikte okunur. Final kapanış için victory/defeat acceptance kanıtı ve oran QA'sı hâlâ gereklidir.
