# Savaş UI cilası — 13 Ekim 2026

Sonuç: kabul kapıları geçti; değişiklikler commit edilmeden ana klasörde bırakıldı.
Başlangıç: main f5131b2, EditMode 213/213, PlayMode 180/180.

## Uygulama

- `Assets/Scripts/Presentation/DefenderKeepPlate.cs:26`: kale modelinin renderer sınırlarından hesaplanan ekran plakası, KALE/KEEP etiketi, can/max sayı ve bar, ≤5 Danger ve bir kez Punch, sızmada kısa kırmızı flaş; kısa telefon ekranında kale sol üstüne kayarak modeli örtmez.
- `Assets/Scripts/Presentation/DefenderRewardPopupPool.cs:115`: mevcut 24 öğelik havuzda Danger renginde -N, ikon gizleme, önceden hazırlanan sayı metinleri, kale plakasının üzerinde konum ve scaled lifetime; yeni Canvas yok.
- `Assets/Scripts/Core/GreyboxCombatants.cs:277`: mevcut tek düşman Canvas içinde ortak pill sprite, koyu zemin, yaklaşık 1 px açık kontur, Danger/Gold bar, 0,4 saniyelik scaled hasar izi, ilk hasara kadar normal bar gizleme; boss barı model üstünde sürekli görünür, Vanguard ikonu korunur.
- `Assets/Scripts/Presentation/DefenderHudController.BattleChrome.cs:17`: hap mesaj şeridi, ince altın kontur, küçük uyarı ikonu, TR/EN tek satır, kısa fade/slide; azaltılmış harekette fade.
- `Assets/Scripts/Presentation/DefenderHudResponsiveLayout.cs:235` ve `DefenderHudController.Frame.cs:47`: alt mesajda 16 pt kenar payı, 64 pt yetenek düğmesi, halkanın altında sayaç, ayrı ve içeride kahraman etiketi.
- `Assets/Tests/PlayMode/BattleUiPolishTests.cs:45`: can, kritik renk, gerçek glyph üretimi, kamera zoomuna bağlı konum, GC ayırmayan kare güncellemesi, popup havuzu, görünürlük/hasar izi/boss, güvenli alan ve dokunma alanı testleri.

Üst can pill'i, oyun kuralları, kamera kadrajı, kale modeli ve inşa akışı değişmedi; HUD builder/prefab değişmediğinden RegenerateHudPrefab gerekmedi ve prefab GUID korunuyor.

## Kapılar

- EditMode TAM: **213/213** — `Builds/battle-ui-edit.xml` ve `.log`.
- İlgili PlayMode: **28/28** — BattleHudFrameTests 9, BattlefieldLookTests 2, AuditLayer2PerfTests 13, yeni BattleUiPolishTests 4; `Builds/battle-ui-play.xml` ve `.log`.
- `Builds/battle-ui-report.json`: **status passed**; performans eşikleri gevşetilmedi.
- Kare güncellemesi, popup yeniden kullanımı ve hasar izi Tick GC kontratları geçti.
- `git diff --check`: temiz; Unity link.xml silme ve dinamik NotoSans font yan etkileri geri alındı.

| Sahne | Boyut | Draw call | SetPass |
|---|---|---|---|
| chapter1-prep | 1280×720 | 318→320 (+0.63%) | 53→55 (+3.77%) |
| chapter1-wave | 1280×720 | 1165→1158 (-0.60%) | 90→90 (+0.00%) |
| chapter10-boss | 1280×720 | 1390→1384 (-0.43%) | 120→117 (-2.50%) |
| chapter1-prep | 1560×720 | 318→320 (+0.63%) | 52→54 (+3.85%) |
| chapter1-wave | 1560×720 | 1165→1158 (-0.60%) | 90→94 (+4.44%) |
| chapter10-boss | 1560×720 | 1390→1384 (-0.43%) | 120→120 (+0.00%) |
| chapter1-prep | 1280×960 | 318→320 (+0.63%) | 50→51 (+2.00%) |
| chapter1-wave | 1280×960 | 1165→1158 (-0.60%) | 90→88 (-2.22%) |
| chapter10-boss | 1280×960 | 1391→1385 (-0.43%) | 117→118 (+0.85%) |

En yüksek artış: draw call **+0.63%**, SetPass **+4.44%**; %5 kapısı içinde; dokuz örnekte kamera konumu ve orthographic size önce/sonra aynı.

## Görüntüler ve tekrar üretim

`Builds/battle-ui-before-*.png` / `Builds/battle-ui-after-*.png`: 1280×720, 1560×720, 1280×960; Bölüm 1 aktif dalga ve Bölüm 10 boss için 12 karşılaştırma görüntüsü, ayrıca 6 hazırlık görüntüsü.
Kale plakası, -1 popup, barlar, uyarı şeridi ve kahraman köşesi görsel olarak incelendi; kırpılma ve kale/üst HUD çakışması yok.
`BATTLE_UI_CAPTURE=1 STEP6_CAPTURE_PHASE=before|after` ile BattlefieldLookTests çekim testini koşmak aynı dosya adlarını üretir; aktif dalga düzeninde hasar ve sızma uygulanır.
Ek 844×390 mantıksal telefon testi güvenli alanı, kale modelini örtmemeyi, ≥44 pt dokunma alanını ve sayaç/düğme ayrımını doğrular.

## Koordinatöre kalan

Tam PlayMode süiti ve simülatör: koordinatöre bırakıldı; cihaz testi yapılmadı.
Kabul kapsamı dışında bir değişiklik veya açık test başarısızlığı yok.
