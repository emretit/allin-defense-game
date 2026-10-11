# Defender Chronicles — Enemies, Waves and Boss

Durum: Faz 5 uygulama ve tasarim notu
Son guncelleme: 2026-10-11

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

Canli arketipler Raider, Vanguard, Regenerator, Support, Siege ve Boss'tur (`Assets/Scripts/Data/GreyboxDefinitions.cs`, `Assets/Scripts/Core/GreyboxCombatants.cs`). Flying/Ranged ve eski `EnemyBase` turevleri asagidaki tasarim/prototip notlaridir; canli dalgalarda ayri ucan/menzilli arketip yoktur.

## Mevcut Dusman Davranislari

Fast rol:

- Hizli, dusuk canli, erken savunma testi.
- Kacinma gibi rastgele mekanikler abartilmadan gosterilmeli.

Tank rol:

- Canli Vanguard fiziksel hasarin %50'sine direnir; dusuk canla hizlanma bu arketipte yoktur (`Assets/Scripts/Core/GreyboxCombatants.cs`, `Assets/Scripts/Combat/BattleRules.cs`).
- Gorsel renk degisimi veya VFX ile tetik noktasi okunmali.

Rejenerasyon rol:

- Zamanla iyilesir.
- Rejenerasyon sonsuz savas yaratmayacak degerlerde tutulur.
- `Heal(...)` artik acik metot olarak kullanilir; negatif hasar hilesi kaldirildi.

Gelecek Flying rol:

- Yol engellemeden etkilenmez.
- Sadece belirli karakollar ve kahraman yetenekleri hedefleyebilir.

## Yeni Tehdit Siniflari

Eski prototip kod temeli (canli davranis `GreyboxEnemy.RunSpecialBehaviour` icindedir):

- `SupportEnemy`: Yakindaki dusmanlari periyodik iyilestirir.
- `SiegeEnemy`: Yakindaki karakollara saldirir.
- `BossEnemy`: Can yuzdesine gore 3 faza gecer ve karakollara baski uygular.

Canli Support, Siege ve Boss production sunumuna baglidir (`Assets/Scripts/Core/GameSessionController.cs`, `Assets/Scripts/Presentation/DefenderPresentationCatalog.cs`).

## Ilk Boss Tasarimi

Canli boss kurali (`Assets/Scripts/Core/GreyboxCombatants.cs`, `Assets/Scripts/Core/GameSessionController.Battle.cs`):

- Faz 1: Temel hizla ilerleme.
- Faz 2: Can ≤%66 olunca tek sefer alti Raider takviyesi; bu fazda hiz artmaz.
- Faz 3: Can ≤%33 olunca hareket hizi ×1.44.
- Fazlar yalniz ileri ve sirali ilerler; bir vurusla 1→3 esigi gecilirse 2 sonra 3 islenir. Iyilesme fazi geri dusurmez ve takviyeyi tekrarlatmaz.
- Karakol bozma faz esiklerine bagli degildir: 1.2 sn hazirlik, 2.5 sn bozma ve saldiri sonrasi 4 sn sayac; faz 3 bu sayaci kisaltmaz. Siege bozmasi 1.5 sn'dir.
- Boss sizmasi normal kale hasarindan kalan cani tek yildiz tavanina indirir (20→7). Can kalirsa tum dusmanlar/bekleyen spawn'lar ve dalgalar tamamlaninca en fazla bir yildizla zafer; can biterse yenilgi olur. Sizma tek basina otomatik yenilgi veya diger aktorleri aninda durdurma degildir (`Assets/Scripts/Combat/BattleRules.cs`, `Assets/Scripts/Core/GameSessionController.cs`).

Karsi oyun:

- Knight son hatta boss yaklastiginda otomatik saldirir; Kalkan Darbesi oyuncu tarafindan zamanlanir.
- Kontrol karakollari boss ilerlemesini yavaslatabilir.
- Oyuncu son savunma hattini onceden hazirlayabilir.

Canli ilk bolge 10 bolumdur; Bolum 1 uc dalga, Bolum 10 yedi normal + bir boss dalgasidir. Bolum 10 son dalgada tek boss'un `startDelay` degeri 36 sn'dir; bu yerel zamanlama diger dalgalara uygulanmaz. Kaynak: `Assets/Resources/FirstRegionCampaign.json`, `Assets/Scripts/Data/FirstRegionCampaign.cs`.

## Dalga Tanim Araci

Tamamlanan veri ekleri:

- `WaveData.threatBudget`
- `WaveData.previewLabel`
- `WaveData.isBossWave`
- `EnemySpawnInfo.entryPointIndex`
- `EnemySpawnInfo.groupId`
- `EnemySpawnInfo.threatCost`
- `EnemySpawnInfo.isSpecialThreat`

Bu alanlar eski ScriptableObject prototipinin genisleme hedefidir. Canli ilk bolge `FirstRegionCampaign.json` → `FirstRegionCampaign.CreateWaves` → `WaveDefinition` / `SpawnGroupDefinition` akisiyla yuklenir (`Assets/Scripts/Data/FirstRegionCampaign.cs`).

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

Canli HUD sonraki dalga tehdit onizlemesini gosterir (`Assets/Scripts/Core/GameSessionController.cs`, `RefreshThreatPreviewForPhase`).

Diger UI hedefleri:

- Sonraki dalganin ana rolleri gosterilir.
- Ozel tehdit ikonla belirtilir.
- Boss dalgasi hazirlik ekraninda acikca isaretlenir.

## Erken Dalga Cagirma

Canli kural:

- Oyuncu hazirlik veya molada dalgayi erken baslatabilir.
- Kalan sureye gore altin odulu verilmez; dugme yalniz molayi keser (`Assets/Scripts/Core/GameSessionController.cs`, `StartNextWave`).
- Kucuk erken cagirma odulu gelecek tasarim hedefidir; beklemeyi tamamen anlamsiz yapmamali.

Eski tasarim onerisi (uygulanmadi; yeni degerler MASTER_TODO Katman 3'te degerlendirilecek):

- `earlyBonus = ceil(remainingSeconds * 1.5)`
- Boss oncesi erken cagirma odulu daha dusuk olabilir.
