# ALLIN: Defense — Map, Route and Outpost System

Durum: Faz 2 ilk uygulama ve tasarim notu
Son guncelleme: 2026-10-12

## Koordinat Standardi

Canli dikey dilim sabit izometrik 3B sunumla XZ zemin duzleminde oynanir; Y yuksekliktir (`Assets/Scripts/Data/CampaignLevelFactory.cs`, `Assets/Scripts/Core/GameSessionController.cs`).

Ilk tasarim hedefleri (asagidaki boyut/secim yaricapi canli runtime siniri degildir):

- 1 Unity unit = 1 tasarim hucresi.
- Yol genisligi hedefi = 1.0-1.5 unit.
- Karakol slot secim yaricapi = 0.85 unit.
- Ilk harita yaklasik 22 x 12 unit alana sigar.
- Kamera harita disina cikmayacak sekilde sinirlanacak.

## Yol Agi Modeli

Canli rota `RuntimeCampaignLevel` / `MapDefinition` noktalari ve `CombatMath` mesafe hesabiyla ilerler (`Assets/Scripts/Data/CampaignLevelFactory.cs`, `Assets/Scripts/Core/CombatMath.cs`, `Assets/Scripts/Core/GreyboxCombatants.cs`).

Eski prototip uygulamasi:

- `PathManager` rota noktalarini tutar.
- Her segmentin uzunlugu hesaplanir.
- Kumulatif mesafe tutulur.
- Dusmanin hedef waypoint index'i ve pozisyonu ile gercek rota ilerlemesi hesaplanir.

Cok girisli bolumler (14-50):

- Ek giris rotalari ana yola tek bir dik T-kavsakta baglanir; kavsaktan sonra ana yolu paylasir. Kavsak noktasi ana yolun %18-80'i arasinda, koselere en az 1,4 unit uzakta secilir.
- Kavsaga dik giris bacagi disinda yan yol, diger tum yollarin merkez cizgisinden en az 2,4 unit uzak kalir (yol kenari 1,92 genis; ~0,5 unit cim kalir). Girisler birbirinden en az 3,5 unit uzaktadir.
- Once sol kenar girisleri denenir; sol kenar doluysa giris ust (z 10,9) veya alt (z 0,6) kenardan gelir.
- Yalniz kavsaga kadar olan parca yeni yol olarak cizilir (`CampaignLevelFactory.OwnedBranchSegments`). Kural `CampaignBranchRoutesJoinPrimaryOnceWithoutOverlap` testiyle 50 bolumde dogrulanir.

- Ana yol ve yan yollar ayni organik kaplama, renk, yukseklik, genislik ve tas kenar uretimini kullanir. Yan yollarda yalniz `OwnedBranchSegments` on eki cizilir; T agzinda giris tarafindaki ana kenar taslari ve ana kaplamaya giren yan kenar taslari atlanir, karsi ana kenar korunur.

Gelecek genisleme:

- Baslangic noktasi
- Bitis noktasi
- Kavsak
- Serit
- Ucan rota

## Rota Ilerlemesi

Eski prototip kod degisikligi (canli `GreyboxEnemy.RouteProgress`, `CombatMath.RouteProgress` kullanir):

- `PathManager.GetRouteProgress(...)` eklendi.
- `EnemyBase.RouteProgress` eklendi.
- `TowerBase` icindeki `First` ve `Last` hedefleme artik dunya X konumu yerine rota ilerlemesini kullanir.

Bu, virajli yollarda kulenin yanlis dusmani "ilk" veya "son" sanmasini engeller.

## Sabit Karakol Noktalari

Canli uygulama (`Assets/Scripts/Data/CampaignLevelFactory.cs`, `Assets/Scripts/Core/GameSessionController.cs`, `Assets/Scripts/Core/GreyboxCombatants.cs`):

- `GreyboxOutpostSlot` ve `GameSessionController` sabit slotlari yonetir; `OutpostSlotManager` / `TowerPlacer` eski prototiptir.
- Ilk bolge haritalarinda 8 stratejik slot vardir.
- Slotlar yol kenarina oturur: slot merkezi en yakin rota merkez cizgisinden 1,58-1,95 unit uzaktadir; hedef uzaklik 1,58 unit'tir (1,62 denemesinde kalan ince aralik icin son gorsel kalibrasyon) (yol kenar taslari ~1,07, platform yaricapi 0,625; mevcut test alt siniri 1,55 korunur). Kaleye 3, dusman kapisina 2 unit'ten fazla yaklasmaz. Uretici once esit dagilima yakin aday arar, sikisan virajlarda pencereyi genisletir (`CampaignLevelFactory.BuildOutpostSlots`); `CampaignOutpostSlotsStayClearOfEveryRoute` iki siniri da 50 bolumde dogrular.
- Environment agac/cam/kaya/cali renderer ayak izi herhangi bir slot merkezine 1,1 unit veya rota merkez cizgisine 1,2 unit yaklasirsa renderer gizlenir. Biome dekor proplari da ayni kurala tabidir; zemin/sirt/backdrop bu dekor filtresinin disindadir. Buyuk foliage ayak izleri ve rota kesismeleri de hesaba katilir.
- Kurulum secili bos slota yapilir; serbest grid kurulumu yoktur.
- Karakol satilinca slot tekrar bosalir.

Ilk slot rolleri:

- `Frontline`: Yol ustunde tutma ve asker uretme kararlarina yakin.
- `Ranged`: Daha guvenli yuksek/arka saldiri noktasi.
- `Control`: Yavaslatma, alan etkisi veya destek icin uygun.

## Karakol Yerlestirme Akisi

Canli akis (`Assets/Scripts/Core/GameSessionController.cs`, `SelectSlot` / `TryBuildOutpost`):

1. Oyuncu bos slota dokunur.
2. Secili slot vurgulanir ve karakol tipleri acilir.
3. Oyuncu karakol tipini secer.
4. Altin yeterliyse karakol secili slota kurulur.
5. Slot doluysa, tip kilitliyse veya altin yetersizse kurulum yapilmaz.
6. Iptal ile secim temizlenir.

Mevcut UI:

- Slot secildiginde karakol secenekleri panelde acilir.
- Kurmadan once maliyet ve rol gorunur (`Assets/Scripts/Presentation/DefenderHudController.cs`).
- Mobilde iki dokunusu gecmeyen hizli kurulum korunacak.

## Karakol Yukseltme Agaci

Canli kural:

- Her temel karakol Sv1 → Sv2 duz guclenir; Sv3 iki uzmanliktan birinin secimidir, toplam alti dal vardir.
- Dal acilma esikleri acilmis kampanya bolumune baglidir: Archer 6, Mage 7, Barracks 8.
- Uzmanlik secimi geri alinmaz; satis ve yeniden kurma alternatif karar olur.
- Uzmanlasma modali savasi pause eder; kapaninca modalin aldigi pause geri acilir ve secili hiz korunur, onceden pause olan oturum pause kalir (`Assets/Scripts/Core/GameSessionController.Modals.cs`, `Assets/Scripts/Presentation/DefenderHudController.Specializations.cs`).
- Barracks -> ShieldGuard veya AssaultGuard (uc asker)
- Archer -> RapidFire veya Sharpshooter
- Mage -> Blast veya Frost
- Yukseltilen/uzmanlasan secili karakolun menzil halkasi hemen guncellenir.

Kaynak: `Assets/Scripts/Data/TowerProgression.cs`, `Assets/Scripts/Core/GreyboxCombatants.cs`, `Assets/Scripts/Core/GameSessionController.Battle.cs`, `Assets/Scripts/Core/GameSessionController.cs` (`TryUpgradeOutpost`, `RefreshSelectedPanel`).

## Satis ve Geri Odeme Kurali

Canli kural:

- Satis iadesi toplam yatirimin (kurulum + Sv2 + varsa uzmanlik) asagi yuvarlanmis %60'idir.
- Hazirlik, mola ve savasta satis serbesttir; pause/zafer/yenilgide savunma degistirilemez.
- Uzmanlik maliyeti toplam yatirima dahildir.
- UI satisi iki dokunusla onaylanir: ilk SAT 3 sn gercek zaman boyunca iade tutarli ONAYLA gosterir, ikinci dokunus satar; zaman asimi/secim/panel/faz degisimi onayi iptal eder. Programatik satis API'si dogrudan calisir (`Assets/Scripts/Presentation/DefenderHudController.Sales.cs`, `Assets/Scripts/Core/GameSessionController.Sales.cs`).

Kaynak: `Assets/Scripts/Core/CombatMath.cs` (`Refund`), `Assets/Scripts/Core/GreyboxCombatants.cs` (`invested`, `SellValue`), `Assets/Scripts/Core/GameSessionController.Battle.cs` (`CanChangeDefence`).

Ekonomi suistimalini azaltmak icin gelecek kural:

- Dalga basladiktan sonra son 5 saniye icinde kurulan karakol tam iade alamaz.
- Devre disi kalmis karakol satilabilir, ama iade dusuk olabilir.

## Karakol Hasari ve Devre Disi Kalma

Ilk boss tasarimi icin kural:

- Karakollar kalici yok olmaz.
- Boss veya kusatma dusmani karakolu gecici susturabilir.
- Canli kusatma bozmasi 1.5 sn, boss bozmasi 2.5 sn'dir; 4 sn boss'un saldiri sonrasi sayacidir, bozma suresi degildir (`Assets/Scripts/Core/GreyboxCombatants.cs`).
- Bozulan kislada uretim ve mevcut askerlerin hareket/saldirisi durur; konuslanmis canli askerler yolu tutmaya devam eder (`GreyboxOutpost.Tick`, `GreyboxFriendlyUnit.Tick` / `CanBlock`).
- Susturulan karakol gorsel ve UI ile acikca belirtilir.
- Kahraman veya destek karakoluyla bozma suresini kisaltma gelecek tasarimdir; canli uygulamada yoktur.

## Kale Muhafizi Konumu

Aktif dikey dilim karari:

- Knight kale kapisi yanindaki tek sabit savunma konumunda durur.
- Harita dokunusu Knight'a hareket komutu vermez.
- Knight yakin dusmanlara otomatik saldirir; oyuncu Kalkan Darbesi'ni zamanlar.
- Eski komuta noktasi siniflari arşiv/prototip kapsaminda kalir ve aktif sahneye baglanmaz.

Bu karar fiziksel iPhone, denge ve dis oyuncu kabul kapilari kapanana kadar yeniden acilmayacak.

## Yol Engelleme Kurallari

Canli kural seti:

- Dusman menzilindeki en yakin canli Knight veya konuslanmis askerle temas edince durup saldirir.
- Kucuk/buyuk dusman icin eszamanli tutma kotasi yoktur; bir asker birden fazla dusmani tutabilir.
- Boss da engelleyiciye saldirirken tamamen durur; zorunlu hareket alt siniri yoktur.
- Ucan dusmanin engeli yok saymasi gelecekteki tasarimdir; canli ucan arketip yoktur.
- Birlikler rota boyunca ayri formasyon noktalarina konuslanir.

Kaynak: `Assets/Scripts/Core/GameSessionController.cs` (`FindNearestBlocker`), `Assets/Scripts/Core/GreyboxCombatants.cs` (`GreyboxEnemy.Tick`, `CanBlock`, `FindBarracksRallyPoint`).

## Etkilesim Katmanlari

Canli dokunma `HandleWorldPointer` ile once UI, sonra Selectable katmanindaki slot/karakol raycast'ini isler; bos alanda secim temizlenir. Dusman veya dost birlik secimi ve ayri yetenek hedefleme modu yoktur (`Assets/Scripts/Core/GameSessionController.cs`).

Gelecek genisleme icin hedef dokunma onceligi:

1. UI
2. Secili hedefleme/aktif yetenek onizlemesi
3. Karakol slotu
4. Var olan karakol
5. Dusman
6. Dost birlik/kahraman
7. Bos harita/dekor

Bu sira, mobilde yanlis dokunmayi azaltmak icin esas alinacak.

## Ilk Dikey Dilim Haritasi

Gri kutu harita hedefi:

- Tek ana yol
- 8 karakol slotu
- 1 sabit kale muhafizi konumu
- 3 kritik savunma bolgesi
- Son bolgede boss baskisi
- Yol boyunca erken, orta ve son savunma kararlarini ayiran virajlar

Kritik bolgeler:

- Bati girisi: Oyuncuya ilk sabit slot ve karakol rolunu ogretir.
- Orta kavsak: Alan kontrolu ve kahraman zamanlamasi ister.
- Son savunma: Boss ve gec dalga baskisini toplar.
