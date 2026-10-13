# Savaş alanı çevresi — 2. tur zenginlik, 2026-10-13

**Durum: görsel ve test kapıları geçti; performans kapısı SetPass tepe değeri nedeniyle başarısız.** Değişiklikler commit edilmedi. Bütçe eşiği gevşetilmedi ve daha iyi sonuç veren bağımsız koşu, başarısız toplu koşunun yerine kullanılmadı.

## Uygulama

- `Assets/Scripts/Presentation/DefenderBattlefieldDressing.cs:256`: kamera alanına yayılan, bağımsız `System.Random` ile deterministik değişken yarıçaplı Poisson ret örneklemesi; rota içi boşluklara ilk küçük kümelerde öncelik; kenarlarda daha sık, rota çevresinde seyrek dağılım. Kabul görüntülerinde 29–40 küme: 1–7 ağaçlı koruluklar ve bağımsız taş/çalılar.
- Üç ağaç silueti × üç yaprak tonu; açık mavi-gri taşlar, çalı/eğrelti, küçük ışıklı mantarlar, kale yanında fenerler ve giriş yanında dikenler. Gövde/taç/taş/çalı tek paletli mesh; mantar/fener ışığı mevcut `fireMaterial` ile ikinci mesh. Küme başına renderer ve gerçek yeni ışık yok; animasyon yok.
- `DefenderBattlefieldDressing.cs:415`: yosun, aynı 128×128 zemin dokusuna yumuşak kenarlı lekeler olarak işleniyor; saydam çimen pass'i kaldırıldı. Yol kenarı lekeleri yolun altında kaldığı için taş bordür, yol rengi ve tekerlek izleri değişmiyor. Yeni palet 8×1 RGBA32, 32 bayt; zemin + yol + palet toplamı **109.256 bayt**, 256 KiB altında.
- `DefenderBattlefieldDressing.cs:203` ve `:225`: rota/yuva/kale/kapı tamponlarına ek olarak üst pill/dalga paneli, sol-alt kahraman/bilgi kartı, sağ-alt başlat düğmesi ve gerçek kale mesh sınırlarından türetilen kale etiketi tamponları. Kameraya veya HUD yerleşimine müdahale yok.
- `Assets/Scripts/Core/GameSessionController.cs:605`: siyah blok iki parçalıydı: üretilmiş `Environment.fbx` içindeki `GateBase` küpü ve kapı direk/üst kirişinin sert dikdörtgen gölgesi. Kaide renderer'ı ve yalnız kapı parçalarının gölge atması kapatıldı; model meshleri/konumları ve sahne ışıkları korunuyor. Eski sabit `BossRune` süs halkası da yeni dağılımlı küçük ışıklı süslerle değiştirildi.
- Lisanslı prefab'daki doğa parçaları ve üretici kodu incelendi; sabit eski konumlar açılmadı. Bu tur, üç silueti ve renkleri iki renderer'a sığdırabilen düşük poligonlu mevcut üretim yaklaşımını genişletiyor; yeni dış/AI görseli veya paket yok.

## Doğrulama

- Tam EditMode **240/240**, `Builds/env2-edit.xml`.
- Hedef PlayMode **55/55**, `Builds/env2-play.xml`: AuditLayer2PerfTests 13/13; BattlefieldLookTests 4/4; BattleRadialInteractionTests 19/19; BattleUiPolishTests 4/4; GreyboxSceneTests 15/15.
- Ayrı tanı koşusu **4/4**, `Builds/env2-diagnostic-play.xml`; normal görsel/test akışını değiştirmeyen `ENV_COUNTER_DIAGNOSTIC=1` seçeneği, yalnız ekran görüntüsü alındıktan sonra Canvas ve süs renderer'larını geçici kapatıp geri açar.
- `Assets/Tests/PlayMode/BattlefieldLookTests.cs`: 1/10/14/24/43 bölümlerinde global Random farklıyken tekrar üretilen meshler eşit; 25–40 küme, en az altı farklı z bandı, yol/yuva/uç nokta/HUD tamponları ve gerçek mesh vertexlerinin test edilen ayak izlerinin içinde kaldığı doğrulandı (yalnız kayan nokta sınırları için 0,0001 birim tolerans). Dokuz çekimde gerçek `Keep Plate` ekran dikdörtgeniyle de kesişim kontrolü var. `GateBase` kapalı ve kapı gölge atması kapalı.
- `GreyboxSceneTests.cs:468,558,635`: birleşik süsler kendi ayak izleriyle denetleniyor; çimen renderer kontrolü, aynı davranışı sağlayan dokulu zemin kontrolüne taşındı. Eski davranış testleri ve perf eşikleri silinmedi/gevşetilmedi.
- Başka işin denge dosyalarına dokunulmadı; bunlardan kaynaklanan test hatası görülmedi. Unity'nin sildiği Addressables link dosyaları ve değiştirdiği dinamik Noto Sans font asset'i geri alındı. `git diff --check` temiz.

## Görüntüler ve görsel inceleme

Aynı çalışma ağacında başlangıç görüntüleri: `Builds/env-round2-before-*.png`; son görüntüler: `Builds/env2-after-*.png`. Her sette Bölüm 1 hazırlık, Bölüm 1 aktif dalga ve Bölüm 10 boss × 1280×720 / 1560×720 / 1280×960; tohum 6006, aynı 100×0,1 saniye savaş ilerlemesi, aynı kamera. Denge değişikliklerinin etkisini ayırmak için ayrıca aynı ağaçtaki başlangıç sayaçları saklandı.

Son dokuz görüntü gözle incelendi: iki sıra/ızgara deseni yok; koruluklar, küçük taş/çalılar ve yosun kameradaki çevreyi dolduruyor, yalnız oynanabilirlik/HUD tamponları açık tutuluyor; ağaçlar ve açık taşlar okunur; kapı arkasındaki sabit siyah dikdörtgen yok; yol hâlâ en büyük, kesintisiz ve en okunur yüzey. Sabit HUD köşeleri ve kale etiketi ağaçları/kaleyi örtmüyor. Etkileşim sırasında açılan mevcut radyal menü ve menzil halkası doğal olarak çevrenin üzerine çizilebilir; bunlar bu turda değiştirilmedi.

## Sayaçlar — değişmeyen d2b0356 bütçesi

`Builds/env2-metrics.json`: **status=failed**. Ham kanıt `Builds/env2-after-metrics.json`; beş yerleşmiş karede maksimum UnityStats sayacı; dokuz kamera konumu/orthographicSize hem d2b0356 hem aynı ağaç başlangıcıyla birebir eşit.

| Sahne | Boyut | Küme | Draw call | SetPass | Üçgen |
|---|---|---:|---|---|---|
| chapter1-prep | 1280×720 | 29 | 320 → 157 (-50.94%) | 55 → 53 (-3.64%) | 34414 → 27476 (-20.16%) |
| chapter1-wave | 1280×720 | 29 | 1157 → 756 (-34.66%) | 89 → 96 (+7.87%) | 57934 → 44144 (-23.80%) |
| chapter10-boss | 1280×720 | 30 | 1382 → 1220 (-11.72%) | 116 → 115 (-0.86%) | 63838 → 58356 (-8.59%) |
| chapter1-prep | 1560×720 | 37 | 320 → 157 (-50.94%) | 53 → 55 (+3.77%) | 34414 → 31908 (-7.28%) |
| chapter1-wave | 1560×720 | 37 | 1157 → 756 (-34.66%) | 99 → 96 (-3.03%) | 57934 → 48576 (-16.15%) |
| chapter10-boss | 1560×720 | 34 | 1382 → 1220 (-11.72%) | 115 → 113 (-1.74%) | 63838 → 58506 (-8.35%) |
| chapter1-prep | 1280×960 | 38 | 320 → 157 (-50.94%) | 53 → 52 (-1.89%) | 34414 → 31262 (-9.16%) |
| chapter1-wave | 1280×960 | 38 | 1157 → 756 (-34.66%) | 87 → 102 (+17.24%) | 57934 → 47930 (-17.27%) |
| chapter10-boss | 1280×960 | 40 | 1383 → 1221 (-11.71%) | 116 → 114 (-1.72%) | 63838 → 60634 (-5.02%) |

Draw call her örnekte **%11,71–50,94**, üçgen **%5,02–23,80** düştü. En yüksek artış **SetPass +%17,24 (87 → 102)**: Bölüm 1 aktif dalga 1280×960; izin verilen en yüksek tam sayı 95. Diğer 26 sayaç/sahne/oran karşılaştırması +%10 sınırında.

## Açık kalan kapı ve tanı

Aynı nihai kodun bağımsız `BattlefieldLookTests` tanı koşusunda aynı 4:3 sahne 94 SetPass (+%8,05), dokuz örneğin tamamı +%10 içinde. Bununla birlikte toplu koşudaki 102 gizlenmedi; makine okunur ana rapor başarısız kalıyor. Görsel yakalama bekleme süresi, örnek sayısı veya eşik değiştirilmedi.

`Builds/env2-diagnostic-play.log` içindeki `ENV2_COUNTER_DIAGNOSTIC`: **tam görüntü 94 → HUD Canvas'ları kapalı 52 → HUD ve yeni koruluklar kapalı 51**. Yeni korulukların bu ölçümde eklediği maliyet bir SetPass; farkın büyük kısmı Canvas çizimlerinde. Bu, toplu test sırası/zamana bağlı HUD batch sıralamasını araştırmak için bir ipucudur; tek başına 102'lik sapmanın kök nedenini kanıtlamaz. HUD bu görevin kapsamı dışında olduğundan orada değişiklik yapılmadı; bütçe kapısının tutarlı biçimde kapanması koordinatörde kalan iştir.

Sayaçlar mobil GPU/FPS garantisi değildir. **Simülatör: koordinatöre bırakıldı.** Tam PlayMode'u da koordinatör çalıştırır. Commit/push, deploy, UGS çağrısı, paket veya dış görsel ekleme yapılmadı.
