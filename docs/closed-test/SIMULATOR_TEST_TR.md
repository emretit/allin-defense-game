# iOS Simulator testi — 6 Eylül 2026

Güncel üç askerlik kışla kontrolü (`4215702`), ayakta duran modeller, yol yerleşimi ve yakın dövüş kanıtları `BARRACKS_TEST_TR.md` içindedir. Aşağıdaki sonuçlar önceki `2376c8d` genel arayüz koşusuna aittir.

**Sonuç:** iPhone 13 / iOS 26.5 simülatöründe kurulum, açılış, savaş kontrolleri ve dalga kaydından dönüş geçti. Kaynak `2376c8d`; sürüm `ct-20260906-01`, uygulama `0.4.0`. Unity CLI ile izole kopyadan dışa aktarıldı, Xcode ile arm64 simülatör uygulaması derlendi. Ortak Unity oturumu kullanılmadı.

## Bulunan ve giderilen hatalar

- Oyuncu derlemesinde `BattleTelemetry` oluşturulurken Unity'nin `Application.isBatchMode` sorgusu fazla erken çalışıyordu. Başlatma `Awake` aşamasına taşındı. Önceki hata ve düzeltilmiş koşu `evidence/simulator-slots-before-fix.log` ve `simulator-slots.log` içinde.
- Kule kartının başlığı görünmüyor, boş nokta açıklamaları üst üste geliyordu. Başlık, rol, açıklama, satış bilgisi ve düğmelerin alanları düzenlendi; boş/dolu kart aynı yerleşimi kullanıyor.
- Öğretim mesajlarının sonu kesiliyordu. İki satır ve sınırlı yazı boyutu uyarlaması eklendi.
- Savaş arka plana alındığında dalga göstergesi 1/3'ten 0/3'e dönüyordu. Duraklatılan savaşın dalga numarası korunuyor.
- Ekran üzerinden otomatik kontroller `DC_UI_ACCEPTANCE=1` ile oyuncu ölçümlerinden ayrılabiliyor; bu bayrak normal giriş ekranını atlamıyor.

## Çalışan uygulamada doğrulama

| Kontrol | Sonuç / kanıt |
|---|---|
| Temiz kayıt, hesapsız giriş | “Oyuna başla” → ilk savaş; `simulator-first-launch.png` |
| Ekrana tıklayarak yer seçimi ve kule satın alma | 230 → 155 → 80 altın; iki okçu; menzil halkası göründü |
| Sekiz savunma noktasında kur, yükselt, sat, tekrar kur ve sat | 8/8; `simulator-slots.log`; bu işlev testi ek altın kullanır |
| Menzilde düşman yokken Kalkan Darbesi | “Yetenek harcanmadı”, hâlâ hazır; `simulator-empty-ability.png` |
| Koyu dış boşlukların kaldırılması ve iki yatay yön | Arazi kenarlara kadar çiziliyor; yol/kapı/kale ve kontroller görünür |
| Bilgi kartı ve öğretim metni | Son paket üzerinden gözle kontrol; `simulator-selection-fixed.png`, `simulator-checkpoint-restored.png` |
| Arka plan → uygulamaya dönüş | Savaş duraklatılmış, can ve dalga korunmuş; `simulator-background-paused.png` |
| Dalga sırasında uygulamayı kapatıp açma | Dalga başı kaydı: 80 altın, 20 can, iki seviye 1 okçu. Savaş içinde görülen 118+ altın taşınmadı; aynı başlangıç kaynakları geri geldi. `simulator-checkpoint-restored.png` |
| Bölüm 10 otomatik savaş | Sekiz dalga, boss, zafer; 13 can. `simulator-boss-smoke.log`, `simulator-telemetry.json` |
| Son değişikliklerin sahne testi | 13/13; `simulator-fix-playmode.xml` |

Boss işlev koşusu 2200 ek test altını ve hızlandırılmış zamanı kullanır. Normal kaynaklı denge kabulünün kanıtı değildir. Eski smoke günlük mesajındaki “seven waves” ifadesi sabittir; gerçek dalga kayıtlarında `level_10_wave_01`–`08` tamamlanmıştır. Son slot, arayüz ve boss günlüklerinde yakalanmamış Unity istisnası yoktur. Bu sınırlı test, 30 dakikalık çökme/bellek kabulü sayılmaz.

Bu turdaki ekran etkileşimleri Türkçe arayüzde yapıldı. İngilizce fiziksel/simülatör ekran denetiminin tamamlandığı iddia edilmiyor. `simctl` ham ekran görüntüleri aygıtın dik framebuffer yönünde saklanabilir; yatay uygulama penceresinin doğrudan görüntüsü `evidence/simulator-battle-window.png` içindedir.

## Güncel paketler ve kalan kabul

Android APK, iOS cihaz Xcode dışa aktarımı/imzasız uygulaması ve simülatör uygulaması aynı son kaynakla yeniden derlendi. Paket boyutları ve özetleri `BUILD_MANIFEST.json` içinde. iPhone 13 simülatöründe **ALLIN: Defense Test** kurulu; başlangıç ekranından devam edilebilir.

Fiziksel iPhone'da imzalı kurulum, 30 dakika FPS/termal/bellek ölçümü ve gerçek yeni oyuncu testi bekliyor. Kullanıcının mevcut Xcode hesabı/profili bu simülatör testi için kullanılmadı; profil bulunmadığı sonucuna varılmadı.
