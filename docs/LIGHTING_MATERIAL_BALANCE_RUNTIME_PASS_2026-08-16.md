# Defender Chronicles — Lighting & Material Balance Runtime Pass

Tarih: 2026-08-16
Kapsam: Greenwatch Valley hazırlık görüntüsü; yol, taş, zemin ve gece ışık dengesi.

## Amaç

Road surface ve road border pass sonrası sahnede taşlar fazla mavi/parlak, yol/zemin dengesi ise hâlâ biraz plastik görünüyordu. Bu pass, gameplay davranışına dokunmadan gece atmosferini daha kontrollü hale getirir.

## Yapılan değişiklikler

Kod:

- `Assets/Scripts/Presentation/DefenderBattlefieldDressing.cs`
- `Assets/Scripts/Presentation/DefenderSceneLook.cs`
- `Assets/Tests/PlayMode/GreyboxSceneTests.cs`

Değişiklikler:

- Road cobble rengi soğuk mavi-gri yerine daha sıcak ve düşük parlaklıktaki taş tonuna çekildi.
- Road surface ve road wear renkleri daha koyu/daha az kontrastlı hale getirildi.
- Ground value patch'leri ve keep read pool biraz koyulaştırıldı.
- Moon key/fill ışıkları yumuşatıldı; gate ember fill daha kontrollü hale getirildi.
- Ambient/fog/post-process değerleri daha az mavi/saturate olacak şekilde güncellendi.
- Lighting profile testi yeni `Moon Fill` intensity/range değerlerine göre güncellendi.

## Kanıt

| Kanıt | Yol | Not |
|---|---|---|
| Kabul edilen lighting/material balance pass | `docs/evidence/runtime/defender-simulator-lighting-material-balance-late-2026-08-16-landscape-readable.png` | Taşlar daha az mavi/parlak; yol-zemin daha sakin |
| Raw capture | `docs/evidence/runtime/defender-simulator-lighting-material-balance-late-2026-08-16-raw.png` | Simulator ham framebuffer çıktısı |
| Compile log | `Logs/lighting-material-balance-compile.log` | Unity batch compile hata vermeden tamamlandı |

## Kalite değerlendirmesi

İyileşenler:

- Yol kenar taşları önceki görüntüye göre daha az mavi parlıyor.
- Yol surface, wear ve zemin patch'leri daha sakin bir value bandına oturdu.
- Gece atmosferi korunurken aşırı soğuk/saturate hissi azaltıldı.
- Low/Medium profillerde shadow-casting point light kullanılmama kuralı korunuyor.

Açık kalanlar:

- Bu hâl hâlâ final lighting değildir; actor/building siluetleri yoğun combat görüntüsünde tekrar test edilmeli.
- 16:9 ve 4:3 capture seti alınmadı.
- Profiler/Frame Debugger ile light/draw-call etkisi ölçülmedi.
- URP shadow distance/resolution/cascade değerleri hâlâ VIS-802 kapsamındaki açık kapıdır.

## TODO etkisi

- VIS-412 artık runtime lighting/material balance açısından `Yapılıyor`.
- VIS-802, VIS-803, VIS-413 ve VIS-415 açık kapı olarak duruyor.
