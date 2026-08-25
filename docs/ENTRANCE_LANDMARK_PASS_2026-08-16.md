# VIS-406 — Greenwatch Enemy Entrance Landmark Pass

Tarih: 2026-08-16
Kapsam: Greenwatch Valley batı düşman girişi için generated gate, spawn ışığı ve çevre tehdit dekoru adayları.
Durum: İncelemede — Unity import/runtime prefab bağlantısı ve cihaz capture kapıları açık.

## Amaç

Mevcut haritada düşman girişinin ilk bakışta bulunmasını sağlamak, fakat bunu yalnız kırmızı/turuncu renk kullanarak değil; kapı silueti, yerdeki spawn şekli, sıcak ışık dili, tehdit kristalleri, yanık scuff ve yol yön işaretleriyle yapmak.

Bu pass gameplay pathing, sekiz build slotu, touch hedefleri veya kamera davranışını değiştirmez. Üretilen dosyalar runtime environment prefabına henüz bağlanmamış aday assetlerdir.

## Üretilen assetler

| Asset | Rol | Dosya | Kaynak |
|---|---|---|---|
| `GreenwatchEntrance_Gate` | Düşman gate silueti, portcullis dişleri, sıcak ember socketleri | `Assets/Art/Models/Generated/GreenwatchEntrance_Gate.fbx` | `ArtSource/Blender/GreenwatchEntrance_Gate.blend` |
| `GreenwatchEntrance_SpawnLight` | Spawn ring, shard, flame wisp ve yön chevronları | `Assets/Art/Models/Generated/GreenwatchEntrance_SpawnLight.fbx` | `ArtSource/Blender/GreenwatchEntrance_SpawnLight.blend` |
| `GreenwatchEntrance_Dressing` | Yanık zemin, jagged rock, charred post ve threat crystal çevre seti | `Assets/Art/Models/Generated/GreenwatchEntrance_Dressing.fbx` | `ArtSource/Blender/GreenwatchEntrance_Dressing.blend` |

## Tasarım kararları

- Gate, yolun batı başlangıcında yüksek ve sıcak bir düşman landmark’ı olarak konumlandı.
- Spawn göstergesi düz halka + shard + flame wisp formlarıyla okunur; yalnız renge bağlı değildir.
- Düşman girişindeki sıcak renkler HUD kritik/tehdit diliyle akraba kalır, ancak dünya objesi olarak yerde/kapıda form taşır.
- Çevre dressing’i ilk yol ve S1 build slotunun üstüne taşmayacak şekilde düşük ve kenara yakın tutuldu.
- FBX import meta dosyalarında collider import kapalı tutuldu; bu assetler path/touch collider üretmemeli.

## Metrikler

Kaynak: `docs/evidence/visual-development/entrance-landmark-metrics-2026-08-16.json`

| Asset | Triangle | Material | Socket | Bütçe sonucu |
|---|---:|---:|---:|---|
| `GreenwatchEntrance_Gate` | 940 | 5 | 3 | Soft budget içinde |
| `GreenwatchEntrance_SpawnLight` | 432 | 3 | 2 | Soft budget içinde |
| `GreenwatchEntrance_Dressing` | 296 | 6 | 0 | Soft budget içinde |
| Toplam | 1668 | — | 5 | Mobil vertical slice için kabul edilebilir aday |

## Görsel kanıt

- Contact sheet: `docs/evidence/visual-development/entrance-landmark-sheet-2026-08-16.png`
- Ölçüm raporu: `docs/evidence/visual-development/entrance-landmark-metrics-2026-08-16.json`

Contact sheet üzerinde S1 ve ilk yol bandı ayrıca işaretlendi. Kapı batıda görünür; S1 halkası ve ilk düz yol bölümü kapanmıyor.

## Açık kapılar

- Unity Editor/CLI mevcut shell’de bulunmadığı için import revalidation çalıştırılamadı.
- Runtime environment prefab bağlantısı yapılmadı.
- 16:9, 20:9 ve tablet oranlarında gerçek sahne capture’ı alınmadı.
- Slot/path/touch regression PlayMode testi bekliyor.
- Gate sıcak ışıklarının gerçek URP lighting/profile davranışı VIS-412’de doğrulanacak.
