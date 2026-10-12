# ALLIN: Defense — Road Border Rhythm Pass

Tarih: 2026-08-16
Kapsam: Greenwatch Valley production runtime yol kenarı taş ritmi.

## Amaç

Road surface pass eski dikdörtgen yol hissini azalttı, fakat yol kenar taşları hâlâ çok düzenli ve mekanik görünüyordu. Bu pass, aynı gameplay path ve collider düzenini koruyarak taş bordürlerin “boncuk boncuk” tekrarını kırar.

## Yapılan değişiklikler

Kod: `Assets/Scripts/Presentation/DefenderBattlefieldDressing.cs`

- `Road Hand-Laid Stone Borders` üretiminde sabit aralıklı taş dizimi yerine düzensiz mesafe kullanıldı.
- Bazı taşlar kontrollü olarak atlanıyor; endpoint ve köşelerde path okunurluğu korunuyor.
- Taş boyutu, yükseklik, yaw ve yan offset değerleri daha fazla değişkenlik aldı.
- Köşe dönüşlerine daha iri cap taşları eklendi.
- Taş materyali lisanslı atlas kopyası yerine düz runtime stone rengine geçirildi; böylece atlas/ışık bağımlılığı azaldı.

## Kanıt

| Kanıt | Yol | Not |
|---|---|---|
| Kabul edilen road border rhythm pass | `docs/evidence/runtime/defender-simulator-road-cobble-rhythm-late-2026-08-16-landscape-readable.png` | Taş ritmi daha kırık; path ve slot okunurluğu korunuyor |
| Compile log | `Logs/road-cobble-rhythm-compile.log` | Unity batch compile hata vermeden tamamlandı |

## Kalite değerlendirmesi

İyileşenler:

- Yol kenarı artık daha az mekanik ve daha az eşit aralıklı.
- Road surface ile taş bordürler birlikte daha organik bir rota hissi veriyor.
- Sekiz slot, kale, giriş ve path hâlâ okunuyor.

Açık kalanlar:

- Taşlar hâlâ biraz mavi/parlak okunuyor; lighting/material pass ile tekrar dengelenmeli.
- Bordürler tek mesh/material olsa da Profiler/Frame Debugger ile draw-call ve overdraw ölçümü hâlâ yapılmadı.
- 16:9 ve 4:3 capture setleri eksik.
- Final kalite için road texture/atlas polish ve ground blend hâlâ gerekli.

## TODO etkisi

- VIS-405 runtime road quality açısından ilerledi.
- VIS-413 ve VIS-415 açık kapı olarak duruyor.
