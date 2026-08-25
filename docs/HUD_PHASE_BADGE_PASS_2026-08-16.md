# Defender Chronicles — HUD Phase Badge Pass

Tarih: 2026-08-16
Kapsam: Hazırlık / savaş / boss durumunu üst bar içinde daha net okunur hale getiren state badge pass'i.

## Amaç

Bu pass'in amacı phase bilgisini düz bir tek satır etiket olmaktan çıkarıp daha oyun-UI hissi taşıyan bir durum rozeti haline getirmekti:

- Hazırlık ve savaş hali tek bakışta ayrılmalı.
- Yaklaşan boss ve aktif boss baskısı için daha sert uyarı tonu hazır olmalı.
- Üst bar içinde yeni bir panel hiyerarşisi kurulmadan, mevcut yerleşim üzerinde okunabilirlik artmalı.

## Yapılan değişiklikler

Kod:

- `Assets/Scripts/Presentation/DefenderHudController.cs`
- `Assets/Scripts/Presentation/DefenderHudResponsiveLayout.cs`

Uygulanan iyileştirmeler:

- `TopBar/Phase` alanı runtime'da küçük bir state badge yüzeyi ve accent stripe ile desteklendi.
- Phase badge artık `DURUM` üst etiketi ve daha açık durum satırı taşıyor.
- Hazırlık için `DALGA ÖNCESİ`, normal savaş için `SAVUNMA HATTI`, boss yaklaşımı için `BOSS YAKLAŞIYOR`, aktif boss için `BOSS BASKISI` dili tanımlandı.
- Badge rengi state'e göre uyarlanıyor; boss durumları daha sıcak tehdit tonuna kayıyor.
- Responsive layout phase badge yüzeyini top bar içindeki phase alanına senkron tutuyor.

## Kanıt

| Kanıt | Yol | Not |
|---|---|---|
| Phase badge pass | `docs/evidence/runtime/defender-simulator-hud-phase-badge-pass-prep-relaunch-landscape-readable.png` | Hazırlık ekranında `DURUM / DALGA ÖNCESİ` rozeti görünür |
| Build log | `Builds/unity-ios-simulator-build.log` | Unity iOS Simulator build geçti |
| Xcode log | `Builds/xcodebuild-ios-simulator.log` | Simulator build/install geçti |

## Kısa değerlendirme

Bu pass sonrası:

- Üst barın phase bölümü daha amaçlı ve taranabilir hale geldi.
- Hazırlık, savaş ve boss state'leri için farklı bir görsel kanal oluştu.
- Threat preview kartı ile birlikte HUD'ın “şu an ne oluyor?” anlatımı güçlendi.

Ama final kalite değil:

- Kısa motion/transition hâlâ yok.
- Badge hâlâ biraz sıkışık; 4:3/tablet yerleşim denetimi açık.
- Preview kartı ile phase badge arasındaki üst bilgi ailesi daha da rafine edilebilir.

## TODO etkisi

- `VIS-605` için somut runtime kanıt üretildi.
- Motion eksik olduğu için `VIS-605` henüz kapanış seviyesinde değildir.
- `VIS-611` ve `VIS-612` erişilebilirlik/oran denetimleri hâlâ gereklidir.
