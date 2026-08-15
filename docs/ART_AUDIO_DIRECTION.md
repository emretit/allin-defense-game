# Defender Chronicles — Art and Audio Direction

Durum: Sanatsal dikey dilim uygulandı
Son güncelleme: 2026-08-16

## Gorsel Konsept Paketi

Tamamlanan paket:

- 1 harita parcasi
- 3 karakol
- 1 kahraman
- 6 düşman rolü
- 1 boss silueti
- 1 production savaş HUD'ı

Hedef:

- Uzaktan okunabilen stylized 3B savunma oyunu.
- Gece mavisi, savunma mavisi, taş, orman, düşman kırmızısı, büyü camgöbeği ve ateş/turuncu ortak paleti.
- Özgün Blender kaynakları ve optimize FBX çıktıları.

## Varlik Uretim Standardi

Model kurallari:

- Pivot zemin ortasinda.
- 1 Unity unit tasarim olcegine uygun boy.
- Karakter siluetleri tepeden/izometrik acida okunur.
- Materyal sayisi sinirli tutulur.
- Takim rengi icin ayrilmis aksan bolgesi olur.
- Sabit kamera için düşük poligon bütçesi kullanılır.
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

## Ses Olay Kataloğu

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
- `hero_attack`
- `outpost_attack`
- `mage_impact`
- `heal_pulse`
- `outpost_disrupted`
- `slow_applied`
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

Geçiş:

- 1,1 saniyelik crossfade.
- Boss gecisinde kisa vurgu kullanilabilir.
- UI, SFX ve müzik ayrı mixer gruplarındadır; pause ayrı snapshot'a geçer.

## Uygulama notu

Aktörlerin mantıksal yaşam döngüsü değişmez. `StylizedActorView` görsel hareket/atak/darbe durumunu oynatır ve ölümde çarpışmasız kısa bir görsel yankı üretir. Hasar, VFX ve ses zamanlamasına bağlı değildir. Tüm kritik görseller ve sesler production kataloğunda doğrulanır.

## Mobil Ses Butcesi

Ilk kurallar:

- Ayni anda cok fazla tekrar eden darbe sesi calmaz.
- Oncelik sirasi: UI kritik, kahraman, boss, dalga, karakol, normal vurus.
- Uzak veya tekrar eden sesler kisilir.
- Dusuk cihazda VFX kadar ses yogunlugu da sinirlanir.
