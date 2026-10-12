# Adım 4b — radyal savaş etkileşimi kabulü

K2/K3 uygulandı: boş yuva → ikon önizlemesi → aynı ikonda onay; seçili kulede aynı UiKit menüsü yükseltme, satış ve iki uzmanlaşma dalını gösterir. İstatistikler tek sol alt UiKit kartındadır. Fiyatlar, denge, uzmanlaşma etkileri ve öğretici koşulları değişmedi.

| Değişiklik | Kaynak |
| --- | --- |
| Tek halka, seçime bağlı önizleme, gerçek satın alma kontrolü, eksik altın ve `RadialRejected` | `Assets/Scripts/Presentation/DefenderHudController.Radial.cs:31` |
| Configure dinleyicilerini kaldırıp yeniden bağlama; dokunulabilir Unaffordable durumu | `Assets/Scripts/UI/Kit/UiRadialMenu.cs:32`, `UiVisualState.cs:48` |
| Kenarda yarım yay, engelden kaçınma, alt sıra yedeği | `Assets/Scripts/UI/Kit/UiRadialLayout.cs:20` |
| Üç saniyelik aynı düğmede satış onayı; uzmanlaşmada mevcut hız/faz geri dönüşü | `DefenderHudController.Sales.cs:26`, `DefenderHudController.Specializations.cs:37` |
| 198×80 pt bilgi kartı; dalga CTA her durumda 208×54 pt | `Assets/Scripts/Presentation/DefenderHudResponsiveLayout.cs:113` |
| Hedefe bağlı altın ok, 1/5 sayacı, Biliyorum / ? ve yetenek halkası | `Assets/Scripts/Presentation/DefenderHudController.Tutorial.cs:60` |
| TR/EN adlar, açıklamalar ve sayısal karşılaştırmalar tek kaynak | `Assets/Scripts/UI/FrontendCopy.cs:105`, `Assets/Scripts/Data/TowerProgression.cs:90` |
| Prefab Card üretimi ve menzil çizgisinin sunum bağlantısı | `Assets/Editor/DefenderPresentationAssetBuilder.cs:1403`, `Assets/Scripts/Core/GameSessionController.cs:2288` |

Öğretici balonu menü açıkken gizlenir; ? menüyü kapatıp rehberi geri çağırır. Reduced-motion açıkken ok ve yetenek nabzı sabittir. Uzmanlaşma karşılaştırması iki sayısal farkı gösterir; kalkan dalında asker canı ve üç askerin toplam canı karşılaştırılır. Yetersiz altında ikon gri, fiyat kırmızı ve kartta N eksik yazısı vardır; ikinci dokunuş satın alma yerine reddedilme olayı üretir. Gerçek kilitli seçenek dokunulamaz kalır.

## Kapılar

- EditMode tam suite: **205/205**, [XML](../../Builds/adim4b-edit.xml).
- PlayMode tam suite: **142/142**, [XML](../../Builds/adim4b-play.xml).
- Baseline 203 EditMode / 135 PlayMode üzerine **2 yeni EditMode ve 7 yeni PlayMode testi**; yeni/değişen testler inşa önizlemesi, yuva değişimi, gerçek ekran içi dünya dokunuşu, eksik altın, yükseltme farkı, satış onayı, uzmanlaşma/hız/faz, metin sığması, kenar halka ve öğretici hedef takibini kapsar.
- iPhone 17 Pro simülatörü: **status=passed, errors=[], knownIssues=[]**, [report.json](../../Builds/FrontendExperience/iOS/report.json); 23 kabul görüntüsü.
- HUD prefab GUID: **d0eb9a43eadfb4d768c166ba3b9a6beb**, korundu; prefab builder ile üretildi.
- `git diff --check`: temiz.
- Build yan etkileri geri alındı: `ProjectSettings.asset`, iOS `addressables_content_state.bin`, `NotoSans-Bold-DC.asset` ve Unity tarafından silinen `link.xml` / meta dosyaları.

Koordinatörün açıklaması: yüzde 18 kapısı kalıcı HUD çerçevesi + açık bilgi kartını kapsar; geçici halka ayrı güvenli alan/hit hedefi kesişim kontrolüne tabidir. Dalga CTA gizlenmez veya küçültülmez. Kart açık ölçümler: **800×450 %16,38; 844×390 %17,92; 1024×768 %7,97**. Rota, sekiz yuva ve kale testleri geçer; kenar halka–HUD/hit hedefi **kesişim=0** (`[RADIAL_FRAME_INTERSECTIONS] 0`).

## Cihaz görüntüleri

![İnşa önizlemesi](../../Builds/FrontendExperience/iOS/battle-build-preview.png)

![Seçili kule halkası](../../Builds/FrontendExperience/iOS/battle-selected-tower.png)

![Yükseltme karşılaştırması](../../Builds/FrontendExperience/iOS/battle-upgrade-preview.png)

![Uzmanlaşma önizlemesi](../../Builds/FrontendExperience/iOS/specialization.png)

![Hedefe bağlı öğretici](../../Builds/FrontendExperience/iOS/battle-Turkish.png)

Açık kabul riski yok. Geçici halka, koordinatörün tanımladığı kapsamda yalnız açıkken rota/yuvayı örtebilir; kalıcı çerçeve ve kart için rota/kale görünürlüğü test edildi. Değişiklikler commit edilmedi.
