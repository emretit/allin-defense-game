# ALLIN: Defense — Road Surface Runtime Pass

Tarih: 2026-08-16
Kapsam: Greenwatch Valley production runtime yol yüzeyi; gameplay path ve collider davranışı değiştirilmedi.

## Amaç

Mevcut yolun büyük dikdörtgen kahverengi parçalar halinde görünmesini azaltmak. Bu pass, eski environment road renderer'larını gizleyip aynı gameplay path üzerinde collider'sız, düşük maliyetli organik yol yüzeyi üretir.

## Yapılan değişiklikler

Kod:

- `Assets/Scripts/Core/GameSessionController.cs`
- `Assets/Scripts/Presentation/DefenderBattlefieldDressing.cs`
- `Assets/Tests/PlayMode/GreyboxSceneTests.cs`

Değişiklikler:

- Production environment içindeki `Road*` renderer'ları gizlendi.
- `Road Organic Earth Surface` adlı runtime kozmetik mesh eklendi.
- Road surface aynı path noktalarını takip eder; yeni path, collider veya touch hedefi eklemez.
- Yol köşeleri ve segmentleri küçük wobble/feather ile daha az dikdörtgen görünecek şekilde üretildi.
- PlayMode kapsamına `Road Organic Earth Surface` varlığı ve collider taşımadığı kontrolü eklendi.

## Görsel iterasyon

| Kanıt | Yol | Karar |
|---|---|---|
| İlk organik yol denemesi | `docs/evidence/runtime/defender-simulator-road-organic-surface-late-2026-08-16-landscape-readable.png` | Reddedildi; yol surface siyaha düştü |
| Materyal düzeltme denemesi | `docs/evidence/runtime/defender-simulator-road-organic-surface-material-late-2026-08-16-landscape-readable.png` | Reddedildi; segmentler hâlâ siyah kaldı |
| Unlit denemesi | `docs/evidence/runtime/defender-simulator-road-organic-surface-unlit-late-2026-08-16-landscape-readable.png` | Reddedildi; shader/normal problemi devam etti |
| Kabul edilen normal düzeltmesi | `docs/evidence/runtime/defender-simulator-road-organic-surface-normals-late-2026-08-16-landscape-readable.png` | Kabul edildi; yol görünür, daha organik ve path okunur |

## Doğrulama

- Unity batch compile geçti: `Logs/road-organic-surface-normals-compile.log`
- iOS Simulator build/run geçti.
- Hazırlık sahnesi screenshot ile kontrol edildi.
- Yeni road surface collider taşımaz; test kapsamına eklendi.

## Kalite değerlendirmesi

İyileşenler:

- Eski büyük blok yol yüzeyi gizlendi.
- Yol yüzeyi artık tekdüze dikdörtgen halı gibi değil.
- Path ve sekiz slot okunurluğu korunuyor.

Açık kalanlar:

- Yol bordür taşları hâlâ sert ve ritmik görünüyor.
- Yol yüzeyi final material/atlas kalitesinde değil; runtime flat mesh.
- 16:9 ve 4:3 capture seti alınmadı.
- Profiler/Frame Debugger ile draw-call etkisi ölçülmedi.
- Road surface, ground value ve lighting pass birlikte tekrar dengelenmeli.

## TODO etkisi

- VIS-405 runtime bağlantı açısından ilerledi.
- VIS-414 regression kapsamı genişledi.
- VIS-413 ve VIS-415 açık kapı olarak duruyor.
