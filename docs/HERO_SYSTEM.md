# Defender Chronicles — Hero System

Durum: Faz 4 uygulama ve tasarim notu
Son guncelleme: 2026-08-15

## Ilk Kahraman Rolu

Ilk kahraman: on saf tank/komutan.

Gorevi:

- Kritik hatta gidip baskiyi emmek.
- Dusmanlari kisa sure durdurmak veya yavaslatmak.
- Karakol savunmasinin yetmedigi anlarda oyuncuya aktif mudahale araci vermek.

Ilk yetenek seti:

- Normal saldiri: Yakin menzilli tek hedef.
- Pasif: Yakindaki dost birliklere kucuk savunma bonusu.
- Aktif 1: Shield Bash, yakin alanda hasar ve kisa kontrol.
- Aktif 2: Rally Cry, komuta noktasindaki birlikleri guclendirme.
- Nihai: Last Stand, kisa sure hasar azaltma ve alan tutma.

## Hareket Komutu

Tamamlanan kod degisikligi:

- `HeroBase.CommandTo(...)` eklendi.
- `HeroCommandController` eklendi.
- Oyuncu komuta noktasina dokununca kahraman o noktaya hareket eder.
- Kule kurma modundayken kahraman komutu verilmez.

Kural:

- Kahraman serbest harita noktasina degil, gecerli komuta noktalarina gider.
- Yeni komut eski hareket emrini degistirir.

## Aggro Davranisi

Ilk kural:

- Kahraman komuta noktasina giderken yakinindaki dusmanlara saldirabilir.
- Otomatik hedef secimi komuta noktasini degistirmez.
- Kahraman dusmani takip etmek icin haritanin baska ucuna suruklenmez.
- Oyuncu yeni komut verirse kahraman hemen o yonde hareket eder.

## Yetenek Hedefleme

Hedefleme tipleri:

- Kendine kullanim
- Kahraman cevresi alan
- Komuta noktasi alan
- Yonlu saldiri
- Dost birlik hedefleme

Mobil kural:

- Yetenek secildiginde onizleme acilir.
- Ikinci dokunus onay, iptal tusu vazgecme olur.
- Menzil disi veya gecersiz hedef net gosterilir.

## Cooldown Arayuzu

Mevcut durum:

- HUD'da kahraman ozel yetenek butonu ve cooldown metni var.

Gelecek durumlar:

- Hazir
- Basili/hedefleniyor
- Beklemede
- Kilitli
- Susturulmus veya bayilmis

Kural:

- Yalnizca renk farki kullanilmaz; sayi, dolum ve ikon durumu da gosterilir.

## XP Kazanimi

Tamamlanan kod degisikligi:

- Kahraman artik her vurus basina XP almaz.
- Normal saldirida hedefi oldururse dusman oduluyle orantili XP alir.

Gelecek XP kaynaklari:

- Oldurme
- Asist
- Dalga tamamlama
- Yan gorev
- Boss safhasi

Bu, kahramanin hizli saldiri ile XP'yi kontrolsuz cogaltmasini engeller.

## Savas Ici Seviye Atlama

Ilk tasarim:

- Seviye atlama kisa ve guvenli bir karar ani yaratir.
- Oyuncuya 2-3 secenek sunulur.
- Secenekler sadece sayisal guc degil, oynanis tercihi verir.

Ornek secenekler:

- Shield Bash alani artar.
- Kahraman komuta noktasina daha hizli gider.
- Yakindaki birlikler daha uzun sure dayanir.

## Bayilma ve Geri Donus

Mevcut durum:

- Kahraman olumu yenilgi yaratmaz.
- Kisa sure sonra geri doner.

Ilk kural:

- Bayilma suresi 10 saniye.
- Geri donunce tam can.
- Oyuncu savas kaybetmez, ama kriz cozum aracini gecici kaybeder.

## Ekipman Yuvalari

Ilk yuvalar:

- Silah
- Zirh
- Tilsim

Kural:

- Silah saldiri tarzini etkiler.
- Zirh hayatta kalma bicimini etkiler.
- Tilsim yetenek veya komuta bonusu verir.
- Ekipman yalnizca "+5 guc" hissine sikismamalidir.

## Loadout Ekrani

Ilk ekran hedefi:

- Kahraman secimi
- Ekipman secimi
- Yetenek ozeti
- Bolum onerilen guc bilgisi
- Son kullanilan yuklemeyi hatirlama

MVP'de tek kahramanla baslanacagi icin ekran once basit tutulacak; coklu kahraman ve ekipman geldikce genisleyecek.
