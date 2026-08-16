# Defender Chronicles — Hero System

Durum: Faz 4 uygulama ve tasarim notu
Son guncelleme: 2026-08-15

## Ilk Kahraman Rolu

Ilk kahraman: on saf tank/komutan.

Gorevi:

- Kale onundeki son hatta baskiyi emmek.
- Dusmanlari kisa sure durdurmak veya yavaslatmak.
- Karakol savunmasinin yetmedigi anlarda oyuncuya aktif mudahale araci vermek.

Aktif dikey dilim yetenek seti:

- Normal saldiri: Yakin menzilli tek hedef.
- Aktif: Shield Bash, yakin alanda hasar ve kisa kontrol.
- Bayilma: 10 saniye sonra ayni savunma konumunda tam canla donus.

## Sabit Muhafiz Davranisi

Kural:

- Knight `(21.7, 0.12, 8.15)` konumunda kale muhafizi olarak baslar.
- Oyuncu harita dokunusuyla hareket komutu vermez.
- Knight 1.65 birim icindeki dusmana otomatik saldirir.
- Yeniden dogdugunda ayni savunma konumuna doner.

## Aggro Davranisi

Ilk kural:

- Knight yalnizca sabit konumunun yakinindaki dusmanlara saldirir.
- Dusmani takip etmek icin savunma konumundan ayrilmaz.
- Oyuncu yalnizca Kalkan Darbesi zamanlamasini kontrol eder.

## Yetenek Hedefleme

Kalkan Darbesi Knight merkezli 3.2 birimlik alanda aninda uygulanir. Ayri hedefleme veya hareket modu yoktur; cooldown HUD'da sayi ve dolumla gosterilir.

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
- Kalkan Darbesi cooldown'i kisalir.
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
