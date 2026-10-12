# ALLIN: Defense — Selected Panel Visual Pass

Tarih: 2026-08-16
Kapsam: Production HUD içindeki seçili karakol panelinin ilk kartlaştırma pass'i.

## Amaç

Bu pass'in amacı seçili yapı panelini düz açıklama bloğundan çıkarıp daha hızlı taranabilen bir mini karta dönüştürmekti:

- Yapı türü ve seviye tek bakışta anlaşılmalı.
- Rol bilgisi açıklama kalabalığı içinde kaybolmamalı.
- Hasar / menzil / hız / yükseltme / satış bilgisi daha düzenli görünmeli.
- Upgrade ve sell butonları aynı panelde daha anlamlı bir hiyerarşi kazanmalı.

## Yapılan değişiklikler

Kod:

- `Assets/Scripts/Core/GreyboxCombatants.cs`
- `Assets/Scripts/Core/GameSessionController.cs`
- `Assets/Scripts/Presentation/DefenderHudController.cs`
- `Assets/Scripts/Presentation/DefenderHudResponsiveLayout.cs`

Uygulanan iyileştirmeler:

- `GreyboxOutpost` üzerinden panel için gerekli yapı verileri doğrudan okunur hale getirildi:
  - level
  - role
  - damage
  - range
  - attack rate
  - disrupted state
- `DefenderHudController` içindeki `SetSelected(...)` akışı düz string yerine seçili outpost/slot nesnesiyle beslenecek şekilde güncellendi.
- `SelectedPanel` içine runtime'da başlık şeridi, ikon, seviye rozeti, rol satırı ve alt bilgi metni eklendi.
- Boş slot ve aktif karakol için ayrı panel sunumu tanımlandı.
- Panel genişliği ve iç oranları responsive layout içinde biraz rahatlatıldı.

## Kanıt

| Kanıt | Yol | Not |
|---|---|---|
| İlk seçili panel pass | `docs/evidence/runtime/defender-simulator-selected-panel-pass-landscape-readable.png` | Kart fikri geldi ama layout çakışıyordu |
| İkinci seçili panel pass | `docs/evidence/runtime/defender-simulator-selected-panel-pass2-landscape-readable.png` | Yerleşim daha iyi ama başlık hâlâ dardı |
| Kabul edilen üçüncü pass | `docs/evidence/runtime/defender-simulator-selected-panel-pass3-landscape-readable.png` | Başlık, seviye ve temel stat blokları daha temiz |
| Compile log | `Builds/unity-selected-panel-pass3.log` | Unity batch compile geçti |

## Kısa değerlendirme

Bu pass sonrası seçili panel:

- Öncekine göre daha oyun-UI hissi veriyor.
- Yapı türü / SV2 / temel stat ilişkisini daha hızlı okutuyor.
- Upgrade ve sell aksiyonlarını daha bağlamlı gösteriyor.

Ama final kalite değil:

- Role line ve ekonomi ipucu hâlâ daha güçlü bir ayrım kazanabilir.
- Gerçek 9-slice materyal seti hâlâ yok.
- Hover/pressed/disabled motion yok.
- Slot boş/selected/upgrade state'iyle daha derin görsel bağ kurulabilir.

## TODO etkisi

- `VIS-609` artık aktif uygulama aşamasında.
- `VIS-602` ve `VIS-603` kapanmadan seçili panel final kalite sayılmaz.
- `VIS-611` erişilebilirlik ve `VIS-613` input regresyon kapıları hâlâ açık.
