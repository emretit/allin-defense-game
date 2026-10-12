# ALLIN: Defense — HUD Threat Preview Pass

Tarih: 2026-08-16
Kapsam: Hazırlık ve dalga geçişlerinde yaklaşan tehdit bilgisini daha erken ve daha kolay taranır göstermek.

## Amaç

Bu pass'in amacı threat preview alanını yalnız tek satırlı bir metin olmaktan çıkarıp, oyuncuya dalga başlamadan önce okunur bir “yaklaşan tehdit” kartı sunmaktı:

- İlk dalga bilgisi hazırlık ekranında görünmeli.
- “Aktif baskı” ve “sıradaki tehdit” durumu birbirinden daha net ayrılmalı.
- Boss tehdidi ileride daha sert vurgulanabilecek bir kart yapısına kavuşmalı.
- Mevcut responsive yerleşim korunmalı; HUD prefab'ı sıfırdan kurulmamalı.

## Yapılan değişiklikler

Kod:

- `Assets/Scripts/Core/GameSessionController.cs`
- `Assets/Scripts/Presentation/DefenderHudController.cs`
- `Assets/Scripts/Presentation/DefenderHudResponsiveLayout.cs`

Uygulanan iyileştirmeler:

- `GameSessionController` içine faza göre preview metnini üreten ortak akış eklendi.
- İlk dalga tehdidi artık hazırlık ekranında otomatik gösteriliyor.
- HUD preview alanı runtime'da küçük başlık + gövde metni + panel yüzeyi taşıyan bir karta dönüştürüldü.
- Kart rengi ve başlığı “sıradaki tehdit / aktif baskı / boss” ayrımına hazırlanacak şekilde güncellendi.
- Responsive layout içinde preview kartı ve mesaj banner'ı birbirinden daha güvenli aralığa taşındı.

## Kanıt

| Kanıt | Yol | Not |
|---|---|---|
| Threat preview prep pass | `docs/evidence/runtime/defender-simulator-hud-threat-preview-pass-prep-landscape-readable.png` | Hazırlık ekranında ilk dalga tehdidi görünür |
| Build log | `Builds/unity-ios-simulator-build.log` | Unity iOS Simulator build geçti |
| Xcode log | `Builds/xcodebuild-ios-simulator.log` | Simulator build/install geçti |

## Kısa değerlendirme

Bu pass sonrası:

- Oyuncu artık dalga başlamadan önce ilk baskının türünü görebiliyor.
- Preview alanı düz bannerdan çok daha okunur bir mini kart hissi veriyor.
- Üst bar ile mesaj metni arasındaki bilgi hiyerarşisi güçlendi.

Ama final kalite değil:

- Kartın dekoratif yüzeyi hâlâ biraz geniş ve ağır duruyor.
- Boss state için ayrı ikon/motion dili henüz yok.
- Combat sırasında aktif baskı görünümü ve 4:3/tablet oran QA hâlâ açık.

## TODO etkisi

- `VIS-606` artık somut runtime kanıta sahip ve inceleme aşamasına taşınabilir.
- `VIS-605` durum-badge/motion pass'i gelmeden preview final kalite sayılmaz.
- `VIS-611` ve `VIS-612` erişilebilirlik/oran denetimleri hâlâ gereklidir.
