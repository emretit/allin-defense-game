# Defender Chronicles — Enemies, Waves and Boss

Durum: Faz 5 uygulama ve tasarim notu
Son guncelleme: 2026-08-15

## Dusman Rol Matrisi

Roller:

- Fast: Erken kacar, menzilli karakol ve hizli komut ister.
- Tank: Yolu tikamadan uzun sure dayanir, tek hedef hasar ister.
- Swarm: Cok sayida gelir, alan hasari ister.
- Ranged: Dost birlikleri uzaktan tehdit eder.
- Flying: Yol engelini yok sayar, ozel hedefleme ister.
- Support: Yakindaki dusmanlari iyilestirir veya guclendirir.
- Siege: Karakollara baski kurar.
- Boss: Fazli, okunakli, karsi oyun firsati olan ana tehdit.

Tamamlanan veri ekleri:

- `EnemyRole`
- `EnemyType.Support`
- `EnemyType.Siege`
- `EnemyType.Boss`

## Mevcut Dusman Davranislari

Fast rol:

- Hizli, dusuk canli, erken savunma testi.
- Kacinma gibi rastgele mekanikler abartilmadan gosterilmeli.

Tank rol:

- Can dusunce hizlanan/sertlesen rol.
- Gorsel renk degisimi veya VFX ile tetik noktasi okunmali.

Rejenerasyon rol:

- Zamanla iyilesir.
- Rejenerasyon sonsuz savas yaratmayacak degerlerde tutulur.
- `Heal(...)` artik acik metot olarak kullanilir; negatif hasar hilesi kaldirildi.

Flying rol:

- Yol engellemeden etkilenmez.
- Sadece belirli karakollar ve kahraman yetenekleri hedefleyebilir.

## Yeni Tehdit Siniflari

Tamamlanan kod temeli:

- `SupportEnemy`: Yakindaki dusmanlari periyodik iyilestirir.
- `SiegeEnemy`: Yakindaki karakollara saldirir.
- `BossEnemy`: Can yuzdesine gore 3 faza gecer ve karakollara baski uygular.

Bu siniflar ilk prototip mantigini kurar; gorsel prefab, animasyon ve denge degerleri sonraki icerik gecisinde baglanacak.

## Ilk Boss Tasarimi

Ilk boss hedefi:

- Faz 1: Yavas, okunakli ilerleme.
- Faz 2: Orta can altinda hiz ve baski artisi.
- Faz 3: Son can bolgesinde karakol bozma saldirisi siklasir.

Karsi oyun:

- Kahraman boss odak komuta noktasina alinabilir.
- Kontrol karakollari boss ilerlemesini yavaslatabilir.
- Oyuncu son savunma hattini onceden hazirlayabilir.

## Dalga Tanim Araci

Tamamlanan veri ekleri:

- `WaveData.threatBudget`
- `WaveData.previewLabel`
- `WaveData.isBossWave`
- `EnemySpawnInfo.entryPointIndex`
- `EnemySpawnInfo.groupId`
- `EnemySpawnInfo.threatCost`
- `EnemySpawnInfo.isSpecialThreat`

Bu alanlar ScriptableObject uzerinden kod degistirmeden daha zengin dalga tanimi yapmaya hazirliktir.

## Dalga Butcesi

Ilk tehdit puani yaklasimi:

- Fast dusman: 1 puan
- Swarm grubu: 1-2 puan
- Tank: 4 puan
- Support: 5 puan
- Siege: 6 puan
- Boss: ozel butce

Zorluk sadece can carpaniyla artmayacak; kompozisyon ve zamanlama asil farki yaratacak.

## Dalga Onizlemesi

Ilk UI hedefi:

- Sonraki dalganin ana rolleri gosterilir.
- Ozel tehdit ikonla belirtilir.
- Boss dalgasi hazirlik ekraninda acikca isaretlenir.

## Erken Dalga Cagirma

Ilk kural:

- Oyuncu dalgayi erken baslatabilir.
- Kalan sureye gore kucuk altin odulu verilir.
- Odul, beklemeyi tamamen anlamsiz yapacak kadar buyuk olmaz.

Onerilen form:

- `earlyBonus = ceil(remainingSeconds * 1.5)`
- Boss oncesi erken cagirma odulu daha dusuk olabilir.
