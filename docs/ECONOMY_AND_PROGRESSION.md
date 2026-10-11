# Defender Chronicles — Economy, Upgrades and Progression

Durum: Faz 6 veri ve tasarim notu
Son guncelleme: 2026-10-11

## Savas Ekonomisi

Ilk surumde savas ici ana kaynak yalnizca altindir.

Altin kullanimi:

- Karakol kurma
- Karakol yukseltme
- Erken diriltme veya acil destek yalniz gelecek tasarim; canli satin alma akisinda yok

Altin kaynaklari:

- Dusman odulu
- Dalga tamamlama bonusu
- Erken dalga cagirma bonusu ve yan hedef odulleri gelecek tasarim; su an verilmez

Canli kaynak: `Assets/Scripts/Core/GameSessionController.cs` (`EnemyRemoved`, `CompleteWave`, `StartNextWave`).

Kural:

- Oyuncu savas sirasinda birden fazla para birimi takip etmek zorunda kalmaz.

## Altin Egrisi

Ilk denge hedefi:

- Baslangic altini oyuncuya 2-3 temel savunma karari aldirmali.
- Ilk iki dalga yanlis kurulumda bile tamamen cezalandirici olmamali.
- Orta dalgada uzmanlasma karari baslamali.
- Son dalga/boss oncesi oyuncu en az bir guclu savunma hatti kurabilmeli.

Canli ilk bolge degerleri:

- Baslangic altini bolume bagli: Bolum 1 = 230, Bolum 10 = 400. `GameSessionController` icindeki 200 yalniz fallback degeridir; kampanya verisi bunu ezer.
- Baslangic cani katalogdan gelir: Bolum 1 ve 10 = 20; diger bolumlerde `CampaignCatalog` can egrisi kullanilir.

Kaynak: `Assets/Resources/FirstRegionCampaign.json`, `Assets/Scripts/Data/CampaignCatalog.cs`, `Assets/Scripts/Core/GameSessionController.cs`.

## Karakol Uzmanlik Dengesi

Kural:

- Uzmanliklar "daha iyi" degil, "farkli cozum" olacak.
- Her uzmanligin guclu dusman rolu ve zayif kaldigi durum bulunacak.

Canli Sv3 dallari: Archer → RapidFire / Sharpshooter, Barracks → ShieldGuard / AssaultGuard, Mage → Blast / Frost. Her kule Sv1 → Sv2 sonrasinda iki daldan birini secer; acilma bolumleri Archer 6, Mage 7, Barracks 8, ek maliyetler sirasiyla 160 / 220 / 180 altindir (`Assets/Scripts/Data/TowerProgression.cs`, `Assets/Scripts/Core/GameSessionController.Battle.cs`).

Canli denge (`Assets/Scripts/Data/TowerProgression.cs`):

- Archer Sv1 hasar 24, hiz 2.5/sn; Sv2 hasar ×1.75 / hiz ×1.44 ile 42 hasar, 3.6/sn.
- Diger kulelerde genel Sv2 hasar ×1.55 / hiz ×1.2; Barracks askerleri bu genel hesabi ezer: Sv1 16 hasar / 110 HP, Sv2 32 hasar / 210 HP ve Sv1/Sv2'de 1/0.375 saldiri/sn.
- Kislada her seviyede uc asker vardir; uzmanlasma bunlarin statlarini degistirir.
- Satis, temel kurulum + Sv2 + varsa dal maliyeti toplam yatiriminin asagi yuvarlanmis %60'ini iade eder (`Assets/Scripts/Core/GreyboxCombatants.cs`, `Assets/Scripts/Core/CombatMath.cs`).

## Savas Ici Guc Secimleri

Gelecek tasarim (canli rastgele guc secimi yok):

- Belirli dalgalardan sonra oyuncuya 3 secenek sunulur.
- Secenekler mevcut karakol ve kahraman yuklemesine gore anlamli olur.
- Secenekler zorunlu en iyi rota yaratmamalidir.

Ornekler:

- Secili Barracks birlikleri daha hizli yeniden dogar.
- Archer tipi karakollar destek dusmanlara oncelik verir.
- Kahraman bir sonraki Shield Bash'i daha genis alanda kullanir.

## Bolum Sonu Odul Akisi

Canli akis zafer/yenilgi, cana gore yildiz, Valor/XP odulu ve tekrar/sonraki bolum/haritaya donustur (`Assets/Scripts/UI/AppFlowController.cs`, `Assets/Scripts/Core/CampaignProgressStore.cs`). Ekipman/acilan icerik maddeleri gelecek tasarimdir.

Hedef siralama:

1. Zafer/yenilgi sonucu
2. Yildiz ve yan hedefler
3. Kazanilan kahraman XP
4. Kazanilan meta para
5. Ekipman veya acilan icerik
6. Tekrar dene, yukleme degistir veya haritaya don

## Meta Para Birimi

Tamamlanan veri ekleri:

- `CurrencyType.BattleGold`
- `CurrencyType.Valor`

Ilk meta kaynak: Valor.

Canli profilde Valor ve heroXP birikir; Valor harcama veya ekipman uretimi yok (`Assets/Scripts/Core/CampaignProgressStore.cs`). Ilk zafer bolum Valor odulu + kazanilan yildiz basina 5 Valor verir; tekrar zaferde yalniz iyilesen yildiz basina 5 Valor, XP ise yalniz ilk zaferde verilir.

Gelecek Valor kullanimi:

- Kahraman ekipman uretimi
- Kalici ama sinirli gelisim
- Yeni uzmanlik veya yan icerik acma

Kural:

- Valor savas ici altinla karismaz.
- Valor kazanimi grind baskisi yaratmayacak hizda tutulur.

## Ekipman Nadirligi

Tamamlanan veri ekleri:

- `EquipmentSlot`
- `EquipmentRarity`
- `EquipmentData`
- `RewardData`

Ilk nadirlikler:

- Common
- Rare
- Epic

Kural:

- Ilk surumde daha fazla nadirlik yok.
- Epic ekipman oyunu bozacak kadar buyuk fark yaratmaz.
- Ekipmanlar taktik tercih uretir.

## Ekipman Ozellik Havuzu

Ilk ozellik tipleri:

- Kahraman saldiri bicimi
- Yetenek cooldown degisimi
- Sabit kale savunmasi bonusu
- Birlik yeniden dogma bonusu
- Belirli karakol uzmanligi sinerjisi

Sinirlar:

- Ayni ekipmanda birden fazla ana mekanik degistiren ozellik olmaz.
- Rastgele uretimde gecersiz slot/ozellik eslesmesi olamaz.

## Zorluk Katmanlari

Ilk zorluklar:

- Normal
- Zor
- Kahramanca

Kural:

- Zorluk sadece HP artisi degildir.
- Yeni dalga kompozisyonu, daha erken ozel tehdit veya farkli yan hedef sunar.

## Yildiz ve Gorev Sistemi

Canli yildiz hesabi kalan can oranina baglidir: ≥%75 uc, ≥%40 iki, pozitif can bir, sifir can sifir yildiz. Boss sizmasi normal hasardan sonra cani tek yildiz tavanina dusurur (20→7); can kalir ve tum dalgalar tamamlanirsa zafer, can biterse yenilgi olur. Kaynak: `Assets/Scripts/Combat/BattleRules.cs`, `Assets/Scripts/Core/GameSessionController.cs`.

Gelecek yildiz/yan gorev hedefleri:

- Bolumu tamamla.
- Belirli sayidan fazla can kaybetme.
- Belirli yan hedefi tamamla.

Yan hedef ornekleri:

- Boss dalgasinda en az bir Kalkan Darbesi kullan.
- Boss dalgasindan once 3 farkli karakol kur.
- Belirli karakol tipiyle kazan.

Kural:

- Yildiz sistemi farkli stratejiyi tesvik eder, zorunlu grind yaratmaz.
