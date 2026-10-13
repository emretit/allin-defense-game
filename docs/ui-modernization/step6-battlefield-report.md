# Adım 6 — Savaş alanı görsel uyumu

Başlangıç: main 29e00e1; EditMode 206/206, PlayMode 155/155.

## Değişiklikler

- Ay mavisi dolgu/ana ışık, altın meşale vurgusu ve sis UiTokens paletine bağlandı; mevcut beş Volume bileşeni korundu, kontrast/doygunluk/vinyet azaltıldı. Kalite gölge kuralları değişmedi.
- 100×100 dünya birimlik mevcut zemin üzerinde 128×128 RGBA32 mipmap dokusu: hafif deterministik gürültü ve kenara doğru koyu mavi geçiş. Yeni doku GPU texel verisi 87.380 bayt (85,33 KiB); disk görseli yok, CPU okunabilir kopyası bırakılmıyor.
- Yol taşları seyrekleştirildi; yol yüzeyi ve zemindeki iki ton daha düşük doygunlukla ayrılıyor. Mevcut sınır taşları/ağaçları korunuyor; kamera ve çarpışma ölçüleri değişmiyor.
- Altı parçalı neon yuva yerine iki paylaşılan mesh: pahlı taş kaide ve ince altın halka. Yalnız seçili yuvanın halkası emisyon kazanıyor ve kule kaidesinin dışına açılıyor; mevcut seç/önizle/kur akışı korunuyor.
- Actor malzeme seçiminde genel Art adı yerine gerçek rol adı kullanılıyor; boss, support ve regenerator sıcak düşman paletine eşleniyor.

## Kanıt düzeni

Builds/step6-before-*.png ve Builds/step6-after-*.png: Bölüm 1 hazırlık, Bölüm 1 aktif dalga, Bölüm 10 boss; 1280×720 (16:9), 1560×720 (19,5:9), 1280×960 (4:3).
Aynı kamera ve sabit random tohumu; savaş 0,1 saniyelik 100 adımla ilerletiliyor ve görüntü sırasında durduruluyor. Boss dalgası test kurulumuyla açılıyor; bu görseller oyun dengeleme kanıtı değildir.
UnityEditor.UnityStats, beş yerleşmiş karede maksimum drawCalls/setPassCalls/triangles; ham örnekler step6-before-metrics.json ve step6-after-metrics.json.

## Kapılar

- EditMode: 206/206, başarısız 0; BarracksPresentationTests 1/1 (Builds/step6-edit.xml).
- PlayMode: 157/157, başarısız 0; GreyboxSceneTests 15/15, AuditLayer2PerfTests 13/13, BattlefieldLookTests 2/2 (Builds/step6-play.xml).
- Simülatör kabulü: iPhone 17 Pro / iOS 26.5, status=passed, errors=[], knownIssues=[], 33 ekran görüntüsü (Builds/FrontendExperience/iOS/report.json); son kaynak yeniden derlenip kuruldu.
- git diff --check: temiz; ProjectSettings/Addressables/dinamik font build yan etkileri geri alındı; commit yapılmadı.
- Fiziksel cihaz FPS: koordinatör/kullanıcı tarafında; burada ölçülmedi.

## Editor ölçümleri

| Sahne | Boyut | Draw calls | SetPass | Üçgen |
|---|---|---:|---:|---:|
| chapter1-prep | 1280×720 | 366 → 286 (-21.86%) | 49 → 49 (+0.00%) | 37860 → 33932 (-10.38%) |
| chapter1-wave | 1280×720 | 1205 → 1125 (-6.64%) | 82 → 76 (-7.32%) | 60826 → 56898 (-6.46%) |
| chapter10-boss | 1280×720 | 1433 → 1353 (-5.58%) | 111 → 102 (-8.11%) | 66836 → 63018 (-5.71%) |
| chapter1-prep | 1560×720 | 366 → 286 (-21.86%) | 48 → 49 (+2.08%) | 37860 → 33932 (-10.38%) |
| chapter1-wave | 1560×720 | 1205 → 1125 (-6.64%) | 75 → 77 (+2.67%) | 60826 → 56898 (-6.46%) |
| chapter10-boss | 1560×720 | 1433 → 1353 (-5.58%) | 108 → 100 (-7.41%) | 66836 → 63018 (-5.71%) |
| chapter1-prep | 1280×960 | 366 → 286 (-21.86%) | 50 → 52 (+4.00%) | 37860 → 33932 (-10.38%) |
| chapter1-wave | 1280×960 | 1205 → 1125 (-6.64%) | 81 → 79 (-2.47%) | 60826 → 56898 (-6.46%) |
| chapter10-boss | 1280×960 | 1433 → 1353 (-5.58%) | 109 → 103 (-5.50%) | 66836 → 63018 (-5.71%) |

Dokuz örnekte kamera konumu/boyutu aynı; bütün sayaç artışları ≤%10 (en yüksek SetPass +%4). Editor sayaçları mobil cihaz GPU süresi/FPS ölçümü değildir.

## Değişen kaynaklar

- Assets/Scripts/Presentation/DefenderSceneLook.cs:61 — ışık, sis ve mevcut Volume ayarları.
- Assets/Scripts/Presentation/DefenderBattlefieldDressing.cs:165 — zemin dokusu, yüzey tonları ve daha seyrek yol taşları.
- Assets/Scripts/Presentation/DefenderSlotVisual.cs:18 — paylaşılan kaide/halka meshleri ve malzemeleri; son yuva kapanınca kaynaklar serbest bırakılır.
- Assets/Scripts/Presentation/StylizedPresentationFactory.cs:46 — üretim yuvası kurulumu; :220 — rol adına göre takım malzemesi.
- Assets/Scripts/Core/GameSessionController.cs:3025 — mevcut SetSelected olayını görsel halkaya ileten bağlantı, oyun kuralı değişikliği yok.
- Assets/Tests/PlayMode/BattlefieldLookTests.cs:36 — üç oranlı görüntü/sayaç kaydı; :97 — seçim, paylaşım, collider ve zemin kapsamı kontrolleri.

## Sınırlar

Kamera kadrajı, HUD builder, paketler, dış görseller ve oyun kuralları değişmedi. Simülatör ve editor ölçümleri fiziksel cihaz FPS/GPU ölçümünün yerine geçmez. Siyah bant geometrik kapsam testi ve görüntü incelemesiyle kontrol edildi; Builds/step6-edge-check.json dokuz resmin sol/sağ 8 piksellik şeritlerini, görüntü yüksekliğinin %20–80 bandında örnekler; boss gölgesinin sınırlı koyu pikselleri bant değildir.
