# Defender Chronicles — Art and Audio Direction

Durum: Faz 8 uretim standardi notu
Son guncelleme: 2026-08-15

## Gorsel Konsept Paketi

Ilk konsept paketi su parcalari icerecek:

- 1 harita parcasi
- 3 karakol
- 1 kahraman
- 4 dusman rol ornegi
- 1 boss silueti
- 1 savas HUD taslagi

Hedef:

- Uzaktan okunabilen stylized 3B savunma oyunu.
- Age of Arena'nin okunabilir izometrik disiplininden faydalanan ama kendi fantastik savunma kimligini kuran bir stil.

## Varlik Uretim Standardi

Model kurallari:

- Pivot zemin ortasinda.
- 1 Unity unit tasarim olcegine uygun boy.
- Karakter siluetleri tepeden/izometrik acida okunur.
- Materyal sayisi sinirli tutulur.
- Takim rengi icin ayrilmis aksan bolgesi olur.
- Mobil icin LOD dusunulur.
- Collider basit tutulur.

Animasyon adlari:

- `Idle`
- `Move`
- `Attack`
- `Hit`
- `Defeat`
- `Ability`
- `Victory`

## Age of Arena Varlik Siniri

Paylasilabilecekler:

- Teknik performans butceleri
- Izometrik kamera disiplini
- Varlik registry yaklasimi
- CC0/provenans takip prensibi

Paylasilmayacaklar:

- Birebir model, karakter, bina veya UI kopyasi
- Ayni renk paletinin aynen kullanimi
- Ayni evren hissini yaratacak isim/hikaye baglantisi

## Ilk Harita Cevre Seti

Gereken parcalar:

- Zemin
- Yol
- Kaya
- Bitki
- Sur/kapi
- Kopru veya dar gecit
- Karakol slot zemini
- Boss alani dekoru

Kural:

- Cevre, karakol slotlarini ve yolu gizlememeli.
- Dekor dokunma hedefiyle karismamali.

## Ilk Karakter Seti

Gerekenler:

- Kahraman
- Yakin dovus birlik
- Menzilli birlik
- Buyu birlik
- Fast rol dusmani
- Tank rol dusmani
- Support rol dusmani
- Siege rol dusmani
- Boss

## Temel Animasyon Seti

Her aktif savas varliginda minimum:

- Idle
- Hareket
- Saldiri
- Darbe
- Olum/bayilma

Kahraman ve boss icin ek:

- Yetenek
- Faz gecisi
- Zafer veya tehdit pozu

## VFX Dili

VFX aileleri:

- Fiziksel: keskin, kisa, beyaz/sari vurgu.
- Ates: sicak renk, yukari hareket.
- Buz: mavi, kirilma ve yavaslama hissi.
- Zehir: yesil, damlama veya kalici alan.
- Kutsal/komuta: altin veya beyaz halka.
- Boss: daha buyuk, yavas ve uyari oncesi okunur.

Kural:

- Renk korlugu icin yalnizca renge guvenilmez; sekil ve hareket de farkli olur.

## Ses Olay Katalogu

Olay kimlikleri:

- `ui_click`
- `ui_error`
- `outpost_build`
- `outpost_upgrade`
- `outpost_sell`
- `unit_spawn`
- `unit_defeat`
- `enemy_hit`
- `enemy_defeat`
- `hero_move`
- `hero_ability`
- `hero_level_up`
- `boss_phase`
- `boss_attack`
- `wave_start`
- `victory`
- `defeat`

## Muzik Durum Sistemi

Muzik durumlari:

- Hazirlik
- Normal savas
- Yogun savas
- Boss
- Zafer
- Yenilgi

Gecis:

- Mumkunse crossfade.
- Boss gecisinde kisa vurgu kullanilabilir.

## Mobil Ses Butcesi

Ilk kurallar:

- Ayni anda cok fazla tekrar eden darbe sesi calmaz.
- Oncelik sirasi: UI kritik, kahraman, boss, dalga, karakol, normal vurus.
- Uzak veya tekrar eden sesler kisilir.
- Dusuk cihazda VFX kadar ses yogunlugu da sinirlanir.
