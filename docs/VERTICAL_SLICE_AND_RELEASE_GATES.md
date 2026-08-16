# Defender Chronicles — Vertical Slice and Release Gates

Durum: Faz 10 kapı notu
Son guncelleme: 2026-08-15

## Gri Kutu Dikey Dilim Kapisi

Tamamlanmis sayilmasi icin:

- 1 harita bastan sona oynanir.
- 3 karakol tipi anlamli rol farki sunar.
- Birlikler savunma hattina katilir.
- Knight kale onunde otomatik savunur ve oyuncu Kalkan Darbesi'ni zamanlar.
- 6 normal dalga ve 1 boss dalgasi calisir.
- Zafer ve yenilgi akislari guvenilirdir.

Mevcut durum:

- Final iOS Simulator yapisi iki ayri hizlandirilmis kabul kosusunda 6 normal dalga ve boss zaferiyle tamamlandi.
- Yenilgi ve temiz sahne restart akisi ayri kabul kosusunda gecti.
- Development runtime kaydinda yakalanmis error veya exception bulunmadi.
- **DC-119 gri kutu kapi kapatildi.** Uc ayri strateji dengesi, gercek parmak stres testi ve 8–12 dakika dis oyuncu sure olcumu DC-120/123 altinda acik kalir.

## Ilk Denge Gecisi Kapisi

Gecis kriterleri:

- En az 3 farkli stratejiyle kazanilabilir.
- Tek zorunlu karakol dizilimi yoktur.
- Oyuncu erken kayip nedenini okuyabilir.
- Boss dalgasi ani ve adaletsiz hissettirmez.

## Sanatsal Dikey Dilim Kapisi

Gecis kriterleri:

- Placeholder karakter, UI, ses ve VFX kalmaz.
- Ilk harita hedef kaliteye yaklasir.
- Kahraman, birlikler, dusmanlar ve boss siluetleri uzaktan okunur.
- Ses ve muzik savas durumlarina tepki verir.

## Performans Kapisi

Gecis kriterleri:

- Dusuk hedef cihazda yogun savas 30+ FPS.
- Tipik cihazda hedef 60 FPS.
- Bellek artisi uzun oturumda kontrol altinda.
- VFX ve ses yogunlugu butceyi asmaz.

## Kullanici Testi Kapisi

Gecis kriterleri:

- Oyuncular temel komutlari yardimsiz anlar.
- Kayip nedeni anlasilir.
- Cogu oyuncu tekrar denemek ister.
- Ilk 3 dalga ogretici gibi calisir, ders ekrani gibi hissettirmez.

## Kampanya Uretim Plani

Dikey dilim basarili olursa planlanacaklar:

- Bolum sayisi
- Biyom sayisi
- Kahraman sayisi
- Dusman aileleri
- Boss sayisi
- Varlik uretim takvimi
- Denge/test dongusu

## Hesap ve Bulut Kayit Karari

Simdilik MVP disi.

Yeniden degerlendirme sarti:

- Tek oyunculu cekirdek keyifli olmali.
- Oyuncu cihazlar arasi ilerleme ihtiyaci gostermeli.
- Kayit modeli surumlu ve migration destekli olmali.

## Liderlik ve Gunluk Gorev Karari

Simdilik MVP disi.

Yeniden degerlendirme sarti:

- Skor kurali hileye dayanikli olmali.
- Gunluk gorevler oyunu is listesine cevirmemeli.
- Tek oyunculu bolum tasarimi zarar gormemeli.

## Gelir Modeli

Ilk etik tercih sirasi:

1. Premium oyun.
2. Ucretli genisleme.
3. Kozmetik IAP.
4. Reklam, yalnizca oyun dengesine dokunmadan.

Kural:

- Pay-to-win yok.
- Enerji sistemi yok.
- Zorunlu reklam yok.

## Magaza Hazirlik Kontrol Listesi

Gerekenler:

- Gizlilik politikasi.
- Yas derecelendirmesi.
- Ekran goruntuleri.
- Kisa tanitim videosu.
- Magaza aciklamasi.
- Destek iletisim kanali.
- Cokme orani takibi.
- Kayit/ilerleme guvenligi.
- Lisans/provenans dosyasi.
