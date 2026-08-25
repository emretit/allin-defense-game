# VIS-407 — Greenwatch Defender Keep Landmark Pass

Tarih: 2026-08-16
Kapsam: Greenwatch Valley final stand bölgesi için generated defender keep, command light ve keep-side dressing adayları.
Durum: İncelemede — Unity import/runtime prefab bağlantısı ve cihaz capture kapıları açık.

## Amaç

Mevcut haritada savunmacı tarafın son hedefini güçlü, dost ve soğuk bir landmark olarak kurmak. Düşman girişindeki sıcak tehdit diline karşılık keep tarafı mavi taş, cyan command magic ve kontrollü gold vurgu ile ayrışmalı.

Bu pass gameplay pathing, sekiz build slotu, touch hedefleri veya kamera davranışını değiştirmez. Üretilen dosyalar runtime environment prefabına henüz bağlanmamış aday assetlerdir.

## Üretilen assetler

| Asset | Rol | Dosya | Kaynak |
|---|---|---|---|
| `GreenwatchKeep_Core` | Ana kale kütlesi, kuleler, banner ve command socketleri | `Assets/Art/Models/Generated/GreenwatchKeep_Core.fbx` | `ArtSource/Blender/GreenwatchKeep_Core.blend` |
| `GreenwatchKeep_CommandLight` | Command ring, beacon ve rally odağı | `Assets/Art/Models/Generated/GreenwatchKeep_CommandLight.fbx` | `ArtSource/Blender/GreenwatchKeep_CommandLight.blend` |
| `GreenwatchKeep_Dressing` | Final stand apron patch, boundary rock, signal post ve cool crystal seti | `Assets/Art/Models/Generated/GreenwatchKeep_Dressing.fbx` | `ArtSource/Blender/GreenwatchKeep_Dressing.blend` |

## Tasarım kararları

- Keep, final stand bölgesindeki en büyük dost kütle olarak tutuldu; giriş gate’inden daha dikey ve daha sakin bir siluet dili kullanıyor.
- Command light yalnız renk parlaması değil; halka, shard, beacon ve altın anchor formuyla okunuyor.
- Dressing, S7/S8 ve boss yaklaşımını kapatmamak için keep çevresinde alçak, kontrollü ve slot dışı yerleştirildi.
- FBX import meta dosyalarında collider import kapalı tutuldu; bu assetler path/touch collider üretmemeli.
- Keep çekirdeğine `Socket_KeepCenter`, `Socket_Banner`, `Socket_CommandLight`, `Socket_Rally` ve `Socket_Focus` eklendi.

## Metrikler

Kaynak: `docs/evidence/visual-development/keep-landmark-metrics-2026-08-16.json`

| Asset | Triangle | Material | Socket | Bütçe sonucu |
|---|---:|---:|---:|---|
| `GreenwatchKeep_Core` | 1728 | 7 | 5 | Soft budget içinde |
| `GreenwatchKeep_CommandLight` | 424 | 3 | 2 | Soft budget içinde |
| `GreenwatchKeep_Dressing` | 560 | 8 | 0 | Soft budget içinde |
| Toplam | 2712 | — | 7 | Landmark kümesi için kabul edilebilir aday |

Not: Keep landmark kümesi `environment prop` bütçesinden değil, final objective landmark kümesi olarak yorumlandı. Bu yüzden soft budget, tekil küçük prop yerine savunma hedefi landmark yoğunluğuna göre değerlendirildi.

## Görsel kanıt

- Contact sheet: `docs/evidence/visual-development/keep-landmark-sheet-2026-08-16.png`
- Ölçüm raporu: `docs/evidence/visual-development/keep-landmark-metrics-2026-08-16.json`

Contact sheet üzerinde S7 ve S8 işaretlendi. Keep ve command light final stand odağını kurarken bu iki slot ile boss yaklaşım alanı okunur kaldı.

## Açık kapılar

- Unity Editor/CLI mevcut shell’de bulunmadığı için import revalidation çalıştırılamadı.
- Runtime environment prefab bağlantısı yapılmadı.
- 16:9, 20:9 ve tablet oranlarında gerçek sahne capture’ı alınmadı.
- Slot/path/touch regression PlayMode testi bekliyor.
- Keep tarafındaki gerçek URP ışık ayarı ve low/medium kalite profili davranışı VIS-412’de doğrulanacak.
