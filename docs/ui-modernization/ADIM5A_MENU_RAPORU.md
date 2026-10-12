# Adım 5a — Ana menü, krallık haritası ve brifing

K1 korundu: menüler dikey, savaş yatay. Kayıt şeması ve paketler değişmedi.

## Uygulama

- `Assets/Scripts/UI/AppFlowController.Screens.cs:58`: UiKit profil çipi/StatPill, dişli IconButton, bölüm Card, üç yıldız, bilgi çipleri ve bölge ilerlemesi; tek altın CTA, Glass Harita/Kahraman eylemleri.
- `Assets/Scripts/UI/AppFlowController.Screens.cs:114`: LayoutGroup ile harita ve ayrılmış BottomSheet alanı; chevron geri düğmesi, gizli tek-bölge sekmesi, mevcut taktik rota önizlemesi, düşman ikon çipleri ve tek satır hedef.
- `Assets/Scripts/UI/AppFlowController.Screens.cs:244`, `Assets/Scripts/UI/FrontendCampaignPageFitter.cs:27`: layout ile madalyonlar, grafik StarRow, kilit ikonları ve altın seçim halkası; rota çizgileri ölçülen düğüm konumlarını izler.
- `Assets/Scripts/UI/AppFlowController.cs:463`: brifing verileri; `:579`: profil/bölüm kartı güncellemeleri; teknik senkron metni yerine yalnız hata ikonu.
- `Assets/Scripts/UI/FrontendCopy.cs:264`: TR/EN menü kimlikleri; tüm on bölümün hedefleri iki dilde en fazla 40 karakter.
- `Assets/Tests/PlayMode/FrontendExperienceTests.cs:194`: dil, ikon/yıldız, Kahraman girişi, kart sınırı ve haritaya alan ayırma regresyon testi; mevcut navigasyon davranış testleri korundu.

## Kapılar

- EditMode: **206/206**, `Builds/step5a-edit.xml` (son kaynak hâli).
- PlayMode: **150/150**, `Builds/step5a-play.xml` (522,09 saniye; baseline 149 + 1 yeni regresyon testi).
- FrontendExperienceTests: **10/10**, `Builds/step5a-frontend.xml`; yeni UI ve mevcut gezinme testleri birlikte geçti.
- Zorunlu gruplar: FrontendExperience **10/10**, FrontendNavigation **6/6**, FrontendCopyBinding **1/1**, UiIdentityAndTouch **4/4**.
- iPhone 17 Pro: `Builds/FrontendExperience/iOS/report.json`, `status=passed`, `errors=[]`, `knownIssues=[]`.
- 33 dokunma hedefi ölçümü yapıldı; minimum **52,11 pt** (eşik 44 pt).
- TR/EN ana menü, harita ve brifingde metin taşması **0**; brifing içeriğinin kart sınırında kaldığı ayrıca ölçülür.
- Son düğüm kaydırılarak görünür; BottomSheet harita viewport'unu örtmez.
- `git diff --check`: temiz.
- Unity kapalı; `link.xml/.meta`, iOS Addressables durumu, `ProjectSettings.asset` ve dinamik NotoSans font yan etkileri geri alındı. Değişiklikler commit edilmedi.

## Ekran görüntüleri

Kök: `Builds/FrontendExperience/iOS/`

| Ekran | Türkçe | İngilizce |
|---|---|---|
| Ana menü | `main-menu-Turkish.png` | `main-menu-English.png` |
| Harita | `map-Turkish.png` | `map-English.png` |
| Brifing | `briefing-Turkish.png` | `briefing-English.png` |
| Son düğüm | `map-last-node-Turkish.png` | `map-last-node-English.png` |

## Sınırlar

Hoş geldin arka planının kaynak çözünürlüğü/büyütmesi bu opsiyonel kapsamda değiştirilmedi. Görsel kabul iPhone 17 Pro üzerinde yapıldı; başka cihazlar için ayrıca görsel kabul koşulmadı.
