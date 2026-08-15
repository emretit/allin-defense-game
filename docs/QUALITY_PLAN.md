# Defender Chronicles — Accessibility, Performance and QA

Durum: Faz 9 kalite kapisi notu
Son guncelleme: 2026-08-15

## Erisilebilirlik Hedefleri

Desteklenecekler:

- Renk korlugu icin sekil/ikon farklari.
- Metin buyuklugu okunabilirligi.
- Titresim kapatma.
- Kamera sarsintisi azaltma.
- Sesli kritik olaylara gorsel geri bildirim.
- Tek elle menu kullanimi.

## Grafik Kalite Profilleri

Profiller:

- Low: Dusuk golge, dusuk parcacik, dusuk cozunurluk olcegi.
- Medium: Dengeli golge, orta parcacik, varsayilan cozunurluk.
- High: Daha iyi golge, daha yogun VFX, yuksek cozunurluk.

Kural:

- Profil degisimi oynanisi degistirmez.
- Dusuk profilde okunabilirlik korunur.

## Performans Butceleri

Hedefler:

- Hedef FPS: 60.
- Kabul edilebilir alt hedef: 30.
- Yogun boss ani: stabil 30+ FPS.
- Gorunur dusman/birlik: ilk dikey dilimde 60 altinda.
- Hasar yazisi ve VFX havuzlanmadan sinirli.
- Yukleme suresi: ilk savas icin 10 saniye alti hedef.

## Referans Cihazlar

Test matrisi:

- Guncel iPhone.
- Eski/orta segment iPhone.
- Dusuk Android: 4 GB RAM sinifi.
- Tipik Android: orta segment guncel cihaz.
- Android tablet.
- iPad.

Editor testleri cihaz testinin yerine gecmez.

## Birim Test Kapsami

Ilk testlenecek kurallar:

- Hasar cozumleme.
- Direnc ve minimum hasar.
- Rota ilerlemesi.
- Hedefleme stratejileri.
- Altin harcama/kazanma.
- XP kaynaklari.
- Cooldown.
- Dalga butcesi.
- Kayit migration.

## Oynanis Entegrasyon Testleri

Senaryolar:

- Dalga baslat.
- Tum dusmanlar temizlenince dalga tamamlanir.
- Dusman kaleye ulasinca can azalir.
- Can sifirlaninca yenilgi olur.
- Tum dalgalar bitince zafer olur.
- Pause ve restart calisir.
- Karakol satilinca slot bosalir.
- Kahraman komuta noktasina gider.

## Kayit Guvenligi

Test dosyalari:

- Yarim yazilmis kayit.
- Eski schema surumu.
- Eksik alanli kayit.
- Bilinmeyen icerik ID'si.
- Bozuk JSON/veri.

Kural:

- Kullanici ilerlemesi mumkun oldugunca korunur.
- Kurtarma olmazsa temiz profil secenegi sunulur.

## Gorsel Regresyon

Sabit goruntuler:

- Ana menu.
- Ilk harita.
- Yogun savas.
- Karakol paneli.
- Zafer.
- Yenilgi.

## Uzun Oturum Testi

Hedef:

- 30-60 dakika kesintisiz oynanis.
- Bellek artisi izlenir.
- Havuz sizintisi izlenir.
- Ses kaybi izlenir.
- Performans dususu izlenir.

## Dokunmatik Stres Testi

Durumlar:

- Hizli ardışık dokunma.
- Coklu dokunma.
- Arka plana alma.
- Cagri/uyari kesintisi.
- Ekran oranı degisimi.
- Pause sirasinda UI kullanimi.

## Denge Simulasyonu

Simule edilecekler:

- Dalga kompozisyonu.
- Karakol hasari.
- Altin akisi.
- Beklenen kazanma orani.
- Boss can egrisi.

## Dis Kullanici Testi

Ilk test:

- 5-10 hedef oyuncu.
- Yardimsiz ilk bolum.
- Komutlari anlama.
- Kayip nedenlerini okuyabilme.
- Tekrar oynama istegi.

## Kritik Hata Cikis Kriteri

Surum engelleyici hatalar:

- Cokme.
- Ilerleme kaybi.
- Savas kilidi.
- Dokunma engeli.
- UI'nin kritik bilgiyi kapatmasi.
- Kayit bozulmasi.
