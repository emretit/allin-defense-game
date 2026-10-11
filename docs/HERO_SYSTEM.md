# Defender Chronicles — Hero System

Durum: Faz 4 uygulama ve tasarim notu
Son guncelleme: 2026-10-11

## Ilk Kahraman Rolu

Ilk kahraman: on saf tank/komutan.

Gorevi:

- Kale onundeki son hatta baskiyi emmek.
- Dusmanlari kisa sure durdurmak veya yavaslatmak.
- Karakol savunmasinin yetmedigi anlarda oyuncuya aktif mudahale araci vermek.

Aktif dikey dilim yetenek seti:

- Normal saldiri: Yakin menzilli tek hedef, 20 hasar / 0.35 sn. Cooldown sifira clamp edilir; bosta bekleme saldiri biriktirmez (`Assets/Scripts/Core/GreyboxCombatants.cs`, `Assets/Scripts/Core/CombatMath.cs`).
- Aktif: Shield Bash, yakin alanda hasar ve kisa kontrol.
- Bayilma: 5 saniye sonra ayni savunma konumunda tam canla donus.

## Sabit Muhafiz Davranisi

Kural:

- Knight bolum haritasinin `guardianPosition` konumunda kale muhafizi olarak baslar; konum rota presetine gore degisir (`Assets/Scripts/Data/CampaignLevelFactory.cs`, `Assets/Scripts/Core/GameSessionController.cs`).
- Oyuncu harita dokunusuyla hareket komutu vermez.
- Knight 1.65 birim icindeki dusmana otomatik saldirir.
- Yeniden dogdugunda ayni savunma konumuna tam canla doner. Menzil ve 5 sn donus kaynagi: `Assets/Scripts/Core/GreyboxCombatants.cs` (`GreyboxHero`).

## Aggro Davranisi

Ilk kural:

- Knight yalnizca sabit konumunun yakinindaki dusmanlara saldirir.
- Dusmani takip etmek icin savunma konumundan ayrilmaz.
- Oyuncu yalnizca Kalkan Darbesi zamanlamasini kontrol eder.

## Yetenek Hedefleme

Kalkan Darbesi Knight merkezli 3.2 birimlik alanda aninda uygulanir. Ayri hedefleme veya hareket modu yoktur; cooldown HUD'da sayi ve dolumla gosterilir. Canli kural: 90 fiziksel hasar, 1 sn stun ve 6 sn cooldown (`Assets/Scripts/Core/GreyboxCombatants.cs`).

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

Canli uygulama (`Assets/Scripts/Core/CampaignProgressStore.cs`, `Assets/Scripts/Core/GreyboxCombatants.cs`):

- Vurus veya oldurme basina kahraman XP verilmez.
- Bir bolum ilk kez tamamlaninca `CampaignCatalog` icindeki `heroXpReward` profilin `heroXp` degerine eklenir; tekrar zafer XP vermez.
- XP birikimi henuz kahraman statlarini veya yetenek secimini degistirmez.

Gelecek XP kaynaklari:

- Oldurme
- Asist
- Dalga tamamlama
- Yan gorev
- Boss safhasi

Bu kaynaklar tasarim hedefidir; canli bolum odul akisina eklenmis degildir.

## Savas Ici Seviye Atlama

Gelecek tasarim (canli savasta secim paneli yok):

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

- Bayilma suresi 5 saniye (`Assets/Scripts/Core/GreyboxCombatants.cs`, `RespawnSeconds`).
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
