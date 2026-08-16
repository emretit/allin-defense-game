# Defender Chronicles — Economy, Upgrades and Progression

Durum: Faz 6 veri ve tasarim notu
Son guncelleme: 2026-08-15

## Savas Ekonomisi

Ilk surumde savas ici ana kaynak yalnizca altindir.

Altin kullanimi:

- Karakol kurma
- Karakol yukseltme
- Gerekirse erken diriltme veya acil destek

Altin kaynaklari:

- Dusman odulu
- Dalga tamamlama bonusu
- Erken dalga cagirma bonusu
- Bazi yan hedef odulleri

Kural:

- Oyuncu savas sirasinda birden fazla para birimi takip etmek zorunda kalmaz.

## Altin Egrisi

Ilk denge hedefi:

- Baslangic altini oyuncuya 2-3 temel savunma karari aldirmali.
- Ilk iki dalga yanlis kurulumda bile tamamen cezalandirici olmamali.
- Orta dalgada uzmanlasma karari baslamali.
- Son dalga/boss oncesi oyuncu en az bir guclu savunma hatti kurabilmeli.

Baslangic degerleri mevcut prototipte korunur:

- Baslangic altini: 200
- Baslangic cani: 20

## Karakol Uzmanlik Dengesi

Kural:

- Uzmanliklar "daha iyi" degil, "farkli cozum" olacak.
- Her uzmanligin guclu dusman rolu ve zayif kaldigi durum bulunacak.

Ornek:

- Shield Guard: Boss ve tank baskisina iyi, suru dalgasina zayif.
- Spear Line: Hizli dusmanlara iyi, ucanlara etkisiz.
- Longbow Nest: Ucan ve destek dusmanlara iyi, yakin baskida zayif.
- Frost Circle: Kalabalik kontrol eder, tek hedef hasari dusuk.

## Savas Ici Guc Secimleri

Ilk kural:

- Belirli dalgalardan sonra oyuncuya 3 secenek sunulur.
- Secenekler mevcut karakol ve kahraman yuklemesine gore anlamli olur.
- Secenekler zorunlu en iyi rota yaratmamalidir.

Ornekler:

- Secili Barracks birlikleri daha hizli yeniden dogar.
- Archer tipi karakollar destek dusmanlara oncelik verir.
- Kahraman bir sonraki Shield Bash'i daha genis alanda kullanir.

## Bolum Sonu Odul Akisi

Siralama:

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

Valor kullanimi:

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

Ilk yildiz hedefleri:

- Bolumu tamamla.
- Belirli sayidan fazla can kaybetme.
- Belirli yan hedefi tamamla.

Yan hedef ornekleri:

- Boss dalgasinda en az bir Kalkan Darbesi kullan.
- Boss dalgasindan once 3 farkli karakol kur.
- Belirli karakol tipiyle kazan.

Kural:

- Yildiz sistemi farkli stratejiyi tesvik eder, zorunlu grind yaratmaz.
