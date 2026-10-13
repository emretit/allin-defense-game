# Savaş alanı çevresi ve inşa yuvaları — 2026-10-13

Başlangıç: main d2b0356, EditMode 213/213, PlayMode 184/184 (koordinatör baseline dosyası).

## Uygulama

- `Assets/Scripts/Presentation/DefenderBattlefieldDressing.cs:355`: gerçek rota değişmeden, görsel merkez çizgisine yuvarlatılmış dönüşler; her rota için birleşik, alçak ve hafif düzensiz taş bordür, düşük kontrastlı iki tekerlek izi ve 64×64 aşınma dokusu.
- `Assets/Scripts/Presentation/DefenderBattlefieldDressing.cs:210`: bağımsız deterministik System.Random ile düzensiz aralıklı kümeler; konik/yuvarlak ve değişken boylu ağaçlar, ortak dört malzeme ve küme başına birleşik taç/gövde/taş meshleri; yol/yuva ve iki uç nokta tamponları, kümeler arasında çakışma kontrolü; 64×64 alfa geçişli çimen. Eski sert yeşil çokgenler ve eşit aralıklı taş/ağaç sıraları kapatıldı.
- `Assets/Scripts/Presentation/DefenderSlotVisual.cs:35`: beyaz görünüm ayrı prefab değildi; mevcut kaide rengi ve uygun fiyat durumundaki Moon emisyonuydu. Koyu taş, ince altın halka, daha büyük altın + ve temas tabanı eklendi; VisualScale=1,38 ve 56-point dokunma alanı korunuyor, seçim halkası genişliyor, kurulumda + gizleniyor; hazırlıkta yavaş nefes, ReducedMotion durumunda ve dalgada statik, dalgada daha düşük emisyon.
- `Assets/Scripts/Core/GameSessionController.cs:620`: mevcut kale/kapı görsellerinin konumları rota uçlarına hizalandı. Özellikle Bölüm 1'de eski kale z=9'da kalırken rota başka z'de bitiyordu; model, rota, collider veya kamera değişmedi. Kale can etiketi mevcut takip davranışıyla kaleyle birlikte taşınır; HUD kodu değişmedi.
- `Assets/Tests/PlayMode/BattlefieldLookTests.cs:163`: 1/10/14/24/43 bölümlerinde iki kez üretim ve farklı global Random durumuyla aynı meshler; yol/yuva/kale/kapı tamponları; kurulumda işaret gizlenmesi; seçim parlaklığı; ReducedMotion ve dalga sakinliği. `GreyboxSceneTests.cs:468` eski üç çimen nesnesinin kontrolünü yeni birleşik çimen nesnesine taşır; davranış testleri ve eşikler korunur.

## Kanıt ve kapılar

- EditMode tam: **213/213**, `Builds/env-edit.xml`.
- PlayMode hedefli: **51/51**, `Builds/env-play.xml`: GreyboxSceneTests 15/15, BattlefieldLookTests 4/4, BattleRadialInteractionTests 19/19, AuditLayer2PerfTests 13/13.
- Yeni iki PlayMode testi başarılı; mevcut seçim testi kurulum sonrası işaret kontrolüyle genişletildi. İlk PlayMode turundaki iki eski nesne adı kontrolü güncellendikten sonra dört sınıfın tamamı yeniden geçti.
- `Builds/env-before-*.png` ve `Builds/env-after-*.png`: Bölüm 1 hazırlık, Bölüm 1 aktif dalga, Bölüm 10 boss; her biri 1280×720, 1560×720, 1280×960, toplam 18 görüntü. Aynı kamera ve 6006 tohum; savaş 100×0,1 saniye ilerletilir. Dokuz son görüntü gözle incelendi: yol tek akış, açık büyük yuvalar, düzenli taş sıraları yok, geometrik siyah ekran bandı yok. Boss sol uçta mevcut kadraj sınırına dokunur; kamera değiştirilmedi.
- Çekim sırasında `ShaderUtil.allowAsyncCompilation=false` kullanılıp sonrasında önceki değer geri yüklenir; aksi halde Editor yeni alfa shader varyantını derlerken geçici cyan dikdörtgen gösterebiliyordu. Bu yalnız test kanıtını yerleşik shader ile çekmek içindir.
- `Builds/env-metrics.json`: **status=passed**, dokuz kameranın konumu ve orthographicSize birebir aynı; beş yerleşmiş karede maksimum UnityStats değerleri. Ham dosyalar `env-before-metrics.json` ve `env-after-metrics.json`.
- Yeni dokular **43.688 bayt** (iki adet 64×64 RGBA32 + mipmap); mevcut 128×128 zemin dahil toplam **131.068 bayt**, 256 KiB sınırından düşük; CPU piksel kopyaları serbest bırakılır.
- Yeni tam ekran pass, paket, dış/AI görseli yok; ışık, fog, Volume ve kalite kuralları değişmedi.
- `git diff --check` temiz; Unity Addressables link ve TMP font yan etkileri geri alındı; commit/push yapılmadı.

## Editor sayaçları

| Sahne | Boyut | Draw calls | SetPass | Üçgen |
|---|---|---|---|---|
| chapter1-prep | 1280×720 | 320 → 209 (-34.69%) | 55 → 53 (-3.64%) | 34414 → 20736 (-39.74%) |
| chapter1-wave | 1280×720 | 1157 → 1046 (-9.59%) | 89 → 93 (+4.49%) | 57934 → 44246 (-23.63%) |
| chapter10-boss | 1280×720 | 1382 → 1271 (-8.03%) | 116 → 112 (-3.45%) | 63838 → 50342 (-21.14%) |
| chapter1-prep | 1560×720 | 320 → 209 (-34.69%) | 53 → 55 (+3.77%) | 34414 → 20736 (-39.74%) |
| chapter1-wave | 1560×720 | 1157 → 1046 (-9.59%) | 99 → 92 (-7.07%) | 57934 → 44256 (-23.61%) |
| chapter10-boss | 1560×720 | 1382 → 1272 (-7.96%) | 115 → 117 (+1.74%) | 63838 → 50364 (-21.11%) |
| chapter1-prep | 1280×960 | 320 → 209 (-34.69%) | 53 → 53 (+0.00%) | 34414 → 20736 (-39.74%) |
| chapter1-wave | 1280×960 | 1157 → 1046 (-9.59%) | 87 → 95 (+9.20%) | 57934 → 44256 (-23.61%) |
| chapter10-boss | 1280×960 | 1383 → 1273 (-7.95%) | 116 → 118 (+1.72%) | 63838 → 50364 (-21.11%) |

En yüksek artış SetPass +%9,20; her sayaç/sahne/oran +%10 sınırında. Draw calls %7,95–34,69, üçgenler %21,11–39,75 azaldı.

## Sınırlar

Bu sayaçlar mobil GPU süresi/FPS ölçümü değildir; fiziksel cihaz performansı burada ölçülmedi. Simülatör: koordinatöre bırakıldı; tam PlayMode ve son kullanıcı görsel kabulü de koordinatör akışındadır. Seçili kule menüsü ve ödül metinleri görüntülerdeki mevcut HUD davranışıdır, bu değişikliğin kapsamında değildir.
