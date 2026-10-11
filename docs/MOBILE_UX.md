# Defender Chronicles — Mobile UI and UX

Durum: Faz 7 tasarim ve kismi uygulama notu
Son guncelleme: 2026-10-11

## Arayuz Tasarim Sistemi

Ilk token seti:

- Panel zemini: koyu yari saydam tas/metal yuzey.
- Ana vurgu: soguk mavi.
- Kaynak vurgu: altin.
- Tehlike: kirmizi/turuncu.
- Basari: yesil.
- Minimum panel radius: 4-8 px.
- Minimum dokunma alani: 48 dp.
- HUD metni: yuksek kontrast, kisa etiket.

## Fantastik HUD Yonu

Hedef:

- Fantastik savunma oyunu hissi.
- Tas, metal ve parsomene yakin malzeme dili.
- FutbolQuiz'den alinacak taraf: buyuk dokunma alanlari, net hiyerarsi, okunabilir sayilar.
- FutbolQuiz'in neo-brutalist gorsel dili kopyalanmayacak.

## Savas HUD Bilgi Mimarisi

Savas ekraninda sabit bilgiler:

- Can
- Altin
- Dalga
- Dalga ilerlemesi
- Hız
- Pause
- Kahraman yetenekleri
- Secili karakol paneli

Kural:

- HUD savas alanini kapatmamalı.
- Kahraman yetenekleri bas parmak erisimi icin alt bolgeye yakin olmali.
- Secili karakol paneli yalnizca secim varken acilmali.

## Dokunma Hedefi Standardi

Kural:

- Kritik butonlar en az 48 dp dokunma alani tasir.
- Ikon kucuk olabilir, ama gorunmez dokunma alani kucuk olamaz.
- HUD ve frontend butonlari en az 112x112 canvas birimi gorunmez raycast dolgusu tasir; 1920x1080 / match 0.5 ve 0.39842 pt/birim orneginde bu en az 44 pt eder. Gorsel boyutlar korunur; cihaz uzerinde kabul ayrica yapilir.
- Slot ve karakol secim yaricaplari mobil parmak hatasini tolere eder.

## Karakol Bilgi Paneli

Panelde gorunecek bilgiler:

- Ad
- Rol
- Seviye
- Hasar
- Menzil
- Saldiri hizi
- Ozel etki
- Yukseltme sonrasi fark
- Satis degeri

Tamamlanan akis:

- SAT ilk dokunusta satis yapmaz; 3 saniye gercek zaman boyunca iade tutariyla ONAYLA +45 / CONFIRM +45 gosterir. Ikinci dokunus satar.
- Zaman asimi, secim degisimi, panel kapanmasi ve faz degisimi onayi iptal eder. Programatik satis API'si dogrudan calisir.
- Uzmanlasma modali mevcut pause mekanizmasiyla savas saatini, dusmanlari, kuleleri ve boss'u durdurur. Kapaninca onceki 1x/2x/4x/8x hiz geri gelir; onceden veya arka plana gecisle pause durumunda olan oturum pause'da kalir. Zafer/yenilgi modali kapatir.

## Kahraman Yetenek Akisi

Kural:

- Kalkan Darbesi butona dokununca Knight merkezinde aninda kullanilir.
- Alan geri bildirimi VFX ile gosterilir.
- Cooldown sayi ve dolumla okunur.

## Ilk Oyun Ogreticisi

Ilk 3 dalga ogretir:

- Dalga 1: Karakol kurma.
- Dalga 2: Sabit slotlarla savunmayi yolun farkli kivrimlarina yayma.
- Dalga 3: Knight Kalkan Darbesi ve dalga onizlemesi.

Kural:

- Uzun metin ekranlari yok.
- Oyuncu oynarken ogrenmeli.

## Ana Menu

MVP ana menu:

- Devam Et
- Bolumler
- Kahraman
- Ekipman
- Ayarlar

MVP disi bos menuler gosterilmez.

## Bolum Secme

Gosterilecekler:

- Harita adi
- Zorluk
- Kazanilan yildizlar
- Onerilen guc
- Odul onizlemesi

## Sonuc Ekranlari

Zafer:

- Yildizlar
- Oduller
- Sonraki bolum
- Tekrar oyna

Yenilgi:

- Kayip nedeni
- Tekrar dene
- Yukleme degistir
- Haritaya don

## Ayarlar

Ilk ayarlar:

- Muzik
- Efekt
- Titresim
- Dil
- Grafik kalite
- Pil modu
- Kamera sarsintisi

## Yerellestirme

Kural:

- Kaynak dil Turkce olabilir.
- Tum kullanici metinleri koddan ayrilacak.
- Dinamik sayilar ve cogullar desteklenecek.
- Yayin hedefinde Ingilizce kaynak da hazirlanacak.
- Bolum 11-50 tehdit metinleri de secili TR/EN dilde uretilir; dil degisimi aktif oturumun dalga onizlemesini savas saatini sifirlamadan yeniler.

## Safe Area ve Ekran Oranlari

Tamamlanan kod degisikligi:

- Uygulama acilisi, ana menu, kampanya ve brifing portre calisir.
- Savas, pause ve savas sonucu iki yatay yonden birinde calisir; haritaya veya ana menuye donuste portreye gecilir.
- `SafeAreaFitter` eklendi.
- Runtime canvas safe area icine oturacak.
- Uretim HUD raylari fiziksel ekran kenarlarina kadar uzanacak.
- Yazi ve dokunma hedefleri raylar icindeki `SafeContent` katmaninda safe area icinde kalacak.
- Savas kamerasi sabit zoom yerine cihaz ve safe area oranina gore yatay kadraji koruyacak.
- Detayli arastirma ve karar kaydi: `docs/UI_FULL_SCREEN_RESEARCH_2026-08-16.md`.

Test edilecek ekranlar:

- Cikintili iPhone ekran
- Standart iPhone yatay ekran
- Uzun Android telefon
- Tablet
- 16:9 yatay ekran
- 20:9 yatay ekran
