# Defender Chronicles — iPhone Cihaz Kabulü

Durum: **Bekliyor — araçlar hazır, manuel fiziksel cihaz koşusu yapılmadı**

## Hedef ve Hazırlık

- Referans cihaz: iPhone 13 (`iPhone14,5`)
- İşletim sistemi: iOS 26.6
- Unity: 6000.4.1f1
- Xcode: 26.6
- Build: Development, debugging ve profiler bağlantısı açık
- Kurulum: `./tools/build_and_run_ios_device.sh`

Bağlı cihaz ve Apple Development imzası sistemde algılandı. Bu kayıt, yalnız aşağıdaki fiziksel testler gerçekten tamamlandıktan sonra “Doğrulandı” durumuna geçirilecek.

## İşlevsel Kabul

- [ ] Soğuk açılıştan hazırlık ekranına 10 saniyeden kısa sürede ulaşır.
- [ ] Oyun iki yatay yönde açılır ve safe area HUD'ı kesmez.
- [ ] Sekiz sabit slotun tamamı görünür ve gerçek parmakla seçilebilir.
- [ ] Slot dışına, dolu slota ve HUD arkasına dokunmak yanlış kurulum yapmaz.
- [ ] Hızlı ardışık ve iki parmaklı dokunma altın/slot durumunu bozmaz.
- [ ] Knight kale önünde otomatik saldırır, hareket etmez ve Kalkan Darbesi çalışır.
- [ ] Arka plana alma, geri dönme, pause ve devam akışları çalışır.
- [ ] Normal hızda 6 dalga + boss 8–12 dakikada tamamlanır.
- [ ] Ayrı yenilgi ve temiz restart koşusu tamamlanır.
- [ ] Crash, error/exception, savaş kilidi veya iOS memory warning oluşmaz.

## Performans Kabulü

`[DC_DEVICE_METRICS]` kayıtları ve Xcode/Unity Profiler birlikte kontrol edilir:

- [ ] Medyan FPS en az 55.
- [ ] 95. yüzdelik frame süresi en fazla 33.3 ms.
- [ ] Bir saniyeden uzun 30 FPS altı dönem yok.
- [ ] Tepe Unity belleği 750 MB altında.
- [ ] Koşu sonu bellek, ilk dalga tabanının en fazla %15 üzerinde.
- [ ] Termal durum `serious` veya `critical` seviyesine çıkmıyor.

## Sonraki Kapılar

- Üç karakol ağırlıklı stratejiyle normal hızlı denge kabulü.
- En az 30 dakikalık veya üç tam bölümlük dayanıklılık koşusu.
- En az 5 dış oyuncuyla yardımsız ilk bölüm testi.
