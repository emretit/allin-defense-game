# Defender Chronicles — Map, Route and Outpost System

Durum: Faz 2 ilk uygulama ve tasarim notu
Son guncelleme: 2026-08-15

## Koordinat Standardi

Ilk dikey dilim 2D sahne duzleminde oynanir ve izometrik/3B sunuma gecene kadar X/Y dunya koordinatlari korunur.

Standartlar:

- 1 Unity unit = 1 tasarim hucresi.
- Yol genisligi hedefi = 1.0-1.5 unit.
- Karakol slot secim yaricapi = 0.85 unit.
- Komuta noktasi secim yaricapi = 0.9 unit.
- Ilk harita yaklasik 22 x 12 unit alana sigar.
- Kamera harita disina cikmayacak sekilde sinirlanacak.

## Yol Agi Modeli

Mevcut waypoint sistemi korunur, ama yol artik yalnizca gorsel cizgi degil, rota ilerlemesi hesaplayan bir modeldir.

Ilk uygulama:

- `PathManager` rota noktalarini tutar.
- Her segmentin uzunlugu hesaplanir.
- Kumulatif mesafe tutulur.
- Dusmanin hedef waypoint index'i ve pozisyonu ile gercek rota ilerlemesi hesaplanir.

Gelecek genisleme:

- Baslangic noktasi
- Bitis noktasi
- Kavsak
- Serit
- Ucan rota
- Komuta noktasi baglantisi

## Rota Ilerlemesi

Tamamlanan kod degisikligi:

- `PathManager.GetRouteProgress(...)` eklendi.
- `EnemyBase.RouteProgress` eklendi.
- `TowerBase` icindeki `First` ve `Last` hedefleme artik dunya X konumu yerine rota ilerlemesini kullanir.

Bu, virajli yollarda kulenin yanlis dusmani "ilk" veya "son" sanmasini engeller.

## Sabit Karakol Noktalari

Tamamlanan kod degisikligi:

- `OutpostSlotManager` eklendi.
- Ilk harita icin 8 stratejik slot tanimlandi.
- `TowerPlacer` once karakol slotu arar; slot sistemi varsa serbest grid yerine slot kullanir.
- Karakol satilinca slot tekrar bosalir.

Ilk slot rolleri:

- `Frontline`: Yol ustunde tutma ve asker uretme kararlarina yakin.
- `Ranged`: Daha guvenli yuksek/arka saldiri noktasi.
- `Control`: Yavaslatma, alan etkisi veya destek icin uygun.

## Karakol Yerlestirme Akisi

Ilk akisin hedefi:

1. Oyuncu karakol tipini secer.
2. Bos slota dokunur.
3. Slot uygunsa onizleme yesil gorunur.
4. Altin yeterliyse karakol kurulur.
5. Slot doluysa veya uzaksa kurulum yapilmaz.
6. Iptal ile secim temizlenir.

Gelecek UI iyilestirmesi:

- Slot secildiginde karakol secenekleri panelde acilacak.
- Kurmadan once maliyet ve rol gorunecek.
- Mobilde iki dokunusu gecmeyen hizli kurulum korunacak.

## Karakol Yukseltme Agaci

Ilk tasarim:

- Her temel karakol 1. seviyeden 2. seviyeye duz guclenir.
- 2. seviyeden sonra iki uzmanlik secenegi acilir.
- Uzmanlik secimi geri alinmaz; satis ve yeniden kurma alternatif karar olur.

Ornek:

- Barracks -> Shield Guard veya Spear Line
- Archer Post -> Longbow Nest veya Marking Post
- Arcane Post -> Frost Circle veya Flame Circle

## Satis ve Geri Odeme Kurali

Ilk kural:

- Satis iadesi temel maliyetin %60'i.
- Dalga sirasinda satis simdilik serbest kalabilir, ama boss ve denge testinden sonra kisitlanacak.
- Uzmanlik maliyetlerinin iadesi ayrica hesaplanacak.

Ekonomi suistimalini azaltmak icin gelecek kural:

- Dalga basladiktan sonra son 5 saniye icinde kurulan karakol tam iade alamaz.
- Devre disi kalmis karakol satilabilir, ama iade dusuk olabilir.

## Karakol Hasari ve Devre Disi Kalma

Ilk boss tasarimi icin kural:

- Karakollar kalici yok olmaz.
- Boss veya kusatma dusmani karakolu gecici susturabilir.
- Susturma suresi 4-8 saniye araliginda tutulur.
- Susturulan karakol gorsel ve UI ile acikca belirtilir.
- Oyuncu kahraman yetenegi veya destek karakoluyla susturmayi kisaltabilir.

## Komuta Noktalari

Tamamlanan kod degisikligi:

- `CommandPointManager` eklendi.
- Ilk harita icin 4 komuta noktasi tanimlandi.

Ilk noktalar:

- Bati tutma noktasi
- Orta tutma noktasi
- Kale kapisi tutma noktasi
- Boss odak noktasi

Gelecek kullanim:

- Kahraman yalnizca bu noktalara gidecek.
- Barracks birlikleri rally hedefi olarak bu noktalari kullanacak.
- Gecersiz dokunuslarda hedef reddedilecek.

## Yol Engelleme Kurallari

Ilk kural seti:

- Kucuk dusman: En fazla 2 dost birlik tarafindan tutulabilir.
- Buyuk dusman: En fazla 3 dost birlik tarafindan tutulabilir, ama yolu tamamen kapatmaz.
- Boss: Tamamen durdurulmaz; yalnizca yavaslatilir veya saldiri animasyonuna sokulur.
- Ucan dusman: Yol engellemeden etkilenmez.
- Birlikler ayni noktaya yigilinca hafif formasyon dagilimi kullanir.

## Etkilesim Katmanlari

Dokunma onceligi:

1. UI
2. Secili hedefleme/aktif yetenek onizlemesi
3. Karakol slotu
4. Var olan karakol
5. Komuta noktasi
6. Dusman
7. Dost birlik/kahraman
8. Bos harita/dekor

Bu sira, mobilde yanlis dokunmayi azaltmak icin esas alinacak.

## Ilk Dikey Dilim Haritasi

Gri kutu harita hedefi:

- Tek ana yol
- 8 karakol slotu
- 4 komuta noktasi
- 3 kritik savunma bolgesi
- Son bolgede boss baskisi
- Yol boyunca erken, orta ve son savunma kararlarini ayiran virajlar

Kritik bolgeler:

- Bati girisi: Oyuncuya ilk karakol/rally mantigini ogretir.
- Orta kavsak: Alan kontrolu ve kahraman zamanlamasi ister.
- Son savunma: Boss ve gec dalga baskisini toplar.
