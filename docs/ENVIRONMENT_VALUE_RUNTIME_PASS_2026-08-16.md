# ALLIN: Defense — Environment Value Runtime Pass

Tarih: 2026-08-16
Kapsam: Greenwatch Valley hazırlık görünümü; gameplay path, slot, collider ve kamera davranışı değiştirilmeden runtime scenic dressing polish'i.

## Amaç

Bu pass, mevcut haritanın “düz oyun tahtası” hissini azaltmak için düşük maliyetli zemin value katmanları ekler. Çalışma final environment kalitesi değildir; VIS-404/VIS-405 için runtime bağlantı ve görsel doğrulama ara adımıdır.

## Yapılan değişiklikler

Kod: `Assets/Scripts/Presentation/DefenderBattlefieldDressing.cs`

- Büyük zemin alanlarını kırmak için üç kozmetik mesh katmanı eklendi:
  - `Ground Low-Contrast Valley Fields`
  - `Ground Moonlit Grass Breakup`
  - `Ground Warm Entry Scuffs`
- Giriş ve kale odağını çok hafif ayırmak için iki düşük kontrast landmark read katmanı eklendi:
  - `Enemy Gate Warm Read Pool`
  - `Defender Keep Cool Read Pool`
- Bu nesneler collider taşımaz; pathing, touch ve tower slot davranışına katılmaz.
- İlk görsel ayar fazla büyük ve lekesel göründüğü için reddedildi; ikinci ayarda renkler koyulaştırıldı ve patch boyutları küçültüldü.

## Kanıt

| Kanıt | Yol | Not |
|---|---|---|
| Reddedilen ilk value pass | `docs/evidence/runtime/defender-simulator-environment-value-pass-late-2026-08-16-landscape-readable.png` | Patch/read pool alanları fazla büyük ve lekesel göründü |
| Kabul edilen ayarlı pass | `docs/evidence/runtime/defender-simulator-environment-value-pass-adjusted-late-2026-08-16-landscape-readable.png` | Zemin daha az düz; path ve slot okunurluğu korunuyor |
| Raw ayarlı capture | `docs/evidence/runtime/defender-simulator-environment-value-pass-adjusted-late-2026-08-16-raw.png` | Simulator ham framebuffer çıktısı |
| Compile log | `Logs/environment-value-pass-adjusted-compile.log` | Unity batch compile hata vermeden tamamlandı |

## Test etkisi

Kod: `Assets/Tests/PlayMode/GreyboxSceneTests.cs`

Production presentation PlayMode testlerine yeni scenic mesh'lerin varlığı ve collider taşımadığı kontrolü eklendi. Unity batch compile geçti; test runner'ın XML üretim problemi önceki notlarda açık kaldığı için bu pass'te tam PlayMode sonucu alınmadı.

## Kalite değerlendirmesi

İyileşenler:

- Büyük düz yeşil alanlar biraz kırıldı.
- West entry, mid bend ve final stand bölgelerinde zemin değeri artık tamamen tek parça değil.
- Yol ve sekiz slot hâlâ okunuyor.
- Eklenen mesh'ler collider taşımadığı için gameplay riski düşük.

Açık kalanlar:

- Bu hâl hâlâ final art direction değil; patch'ler runtime procedural renk katmanı, gerçek atlas/material polish değil.
- Yol yüzeyi hâlâ çok dikdörtgen; road surface/edge/wear generated FBX'lerinin catalog/prefab bağlantısı hâlâ tam kapanmadı.
- 16:9 ve 4:3 capture seti hâlâ alınmadı.
- Profiler/Frame Debugger ile draw-call ve overdraw etkisi ölçülmedi.
- Lighting/value pass, actor/building siluetleriyle birlikte tekrar dengelenmeli.

## TODO etkisi

- VIS-404 ve VIS-405 runtime bağlantı açısından ilerledi; hâlâ final kapanış değil.
- VIS-414 için visual-only collider regresyonu test kapsamına eklendi.
- VIS-413, VIS-415, VIS-412 açık kapı olarak duruyor.
