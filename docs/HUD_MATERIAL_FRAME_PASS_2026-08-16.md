# Defender Chronicles — HUD Material / Frame Pass

Tarih: 2026-08-16
Kapsam: Production HUD ray ve panellerine runtime panel/frame sprite katmanı bağlama pass'i.

## Amaç

Bu pass'in amacı mevcut okunabilir HUD'u daha “oyun UI” hisli hale getirmekti:

- Üst ve alt raylar düz koyu şerit gibi görünmemeli.
- Seçili panel ve sonuç paneli yalnız tint değil, çerçeve ve yüzey hissi taşımalı.
- Mevcut responsive layout korunmalı; sıfırdan prefab kurulumu yapılmamalı.

## Yapılan değişiklikler

Kod:

- `Assets/Scripts/Presentation/DefenderHudController.cs`

Uygulanan iyileştirmeler:

- `DefenderPresentationCatalog` içindeki `panelSprite` ve `uiFrameSprite` runtime’da HUD stil akışına bağlandı.
- TopBar, BottomBar, SelectedPanel ve ResultPanel için sprite tabanlı panel yüzeyi uygulandı.
- Ray dışındaki panellere düşük opaklıklı frame overlay eklendi.
- Selected panel header katmanı da aynı yüzey diliyle uyumlu hale getirildi.

## Kanıt

| Kanıt | Yol | Not |
|---|---|---|
| HUD material/frame pass | `docs/evidence/runtime/defender-simulator-hud-material-pass-landscape-readable.png` | Seçili panel ve raylarda frame/panel yüzeyi görünür |
| Compile log | `Builds/unity-hud-material-pass.log` | Unity batch compile geçti |

## Kısa değerlendirme

Bu pass sonrası:

- Selected panel daha bitmiş bir UI elemanı gibi okunuyor.
- Ray/panel dili birbirine biraz daha yaklaştı.
- Mevcut selected panel kartı önceki çıplak kutu hissinden uzaklaştı.

Ama final kalite değil:

- Frame overlay özellikle seçili panelde hâlâ biraz ağır.
- Top/bottom rail için daha özgün materyal ayrımı yapılabilir.
- Buton yüzeyleri ve panel yüzeyleri arasında hâlâ daha tutarlı bir aile hissi kurulabilir.
- 4:3 ve küçük ekran kontrast testi açık.

## TODO etkisi

- `VIS-602` artık somut runtime kanıta sahip.
- `VIS-609` ile birlikte selected panel polish zinciri güçlendi.
- Final materyal kapanışı için daha ince alpha/border tuning ve oran QA hâlâ gerekli.
