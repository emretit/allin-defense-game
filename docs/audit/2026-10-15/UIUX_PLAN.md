# ALLIN: Defense v1.1 — UI/UX uygulama planı · 2026-10-15

İki denetimdeki (Codex: 0 P0 · 9 P1 · 4 P2 · Claude: 3 P0 · 10 P1 · 10 P2) bulgular ve bilinen açıklar **40 tekil satıra** indirildi; her biri bir pakete ya da gerekçeli "Emre kararı / kapsam dışı" satırına bağlı.
**12 paket, 4 dalga:** wave 1 = WP1 bilgi kartı · WP2 dalga akışı · WP3 erişilebilirlik altyapısı (üçü de ilk dalga düğmesi worker'ının dosyalarına dokunmaz).
Kod okundu, ownership kodla doğrulandı (partial dosyalar dahil); Unity/simülatör koşulmadı. Spec taslakları: `.claude/ui-specs/v11/wp*.txt`.

## 0. Ortak kurallar (tüm paketler)

- `.claude/ui-specs/impl-common.txt` ZORUNLU (unity-lock, link.xml geri alma, mantık testleri `-nographics`, simülatör yok).
- **W0 — koordinatör ön adımı (~2 dk, wave 1'den önce):** `Assets/Scripts/UI/FrontendCopy.cs:33` `public static class FrontendCopy` → `public static partial class FrontendCopy`.
  Böylece her paket yeni TR/EN metnini **kendi** `Assets/Scripts/UI/Copy/FrontendCopy.<Paket>.cs` partial dosyasına yazar. Tek assembly (`AllinDefense.Runtime.asmdef`) olduğu için partial çalışır.
  `UiInfrastructureTests.TenSeededRandomCopyIdsHaveBothLanguages` (`Assets/Tests/EditMode/UiInfrastructureTests.cs:53`) yeni alanları da tarar. `FrontendCopy.cs`'ye yalnız WP6 yazar; var olan bir metni değiştirmek isteyen paket yeni anahtar açar ve kendi çağrı yerini ona bağlar.
- **Sözlük (yeni metinler buna uyar, WP6 eskileri buna çeker):** EN İngiliz yazımı (Defence, Armour, specialise/specialisation), "Chapter" (Level değil), "Region". TR: "Bölüm", "Bölge". Sayı içeren kilit metinlerinde ek almayan kalıp: "{0}. bölümü tamamla".
- **Başlamadan:** `git status --short` → Ownership dosyalarından biri commit'siz başka değişiklik taşıyorsa dur, `ask`.
- **Ownership dışı** bir dosya gerekirse ya da ownership dışı bir test kırılırsa: düzeltme, `ask` ile koordinatöre yaz.
- **Denetim:** her dalga sonunda koordinatör kendi EditMode tam + ilgili PlayMode koşusunu yapar; "yazan denetlemez" (Codex yazdıysa Claude denetler, tersi). Commit Emre onayıyla.
- Baseline: EditMode 283/283 (`Builds/coord/v1-final-edit.xml`); tam grafikli PlayMode 229/230, tek kırmızı BattlefieldLookTests biome SetPass (WP10).

## 1. Tekilleştirilmiş bulgu tablosu (40)

Öncelik iki rapordan yüksek olanı alır. "ikisi" = iki raporda da var. "bilinen" = koordinatör/TODO listesi.

| # | Bulgu | Kaynak | Ö. | Kanıt (file:line) | Paket |
|---|---|---|---|---|---|
| 1 | Bilgi kartı/halka yazıları 9–12 pt; SAT/fiyat küçük; mesaj ve ipucu ellipsis ile kesiliyor | ikisi (cl P0-1, cx P1-04) | P0 | `DefenderHudController.Radial.cs:177-181,:193,:276,:280`; `DefenderHudResponsiveLayout.cs:112-113,:235`; `DefenderHudController.BattleChrome.cs:31` | WP1 |
| 2 | Uzmanlaşma önizlemesinde dalın asıl işlevi (Keskin Nişancı hedef önceliği/menzil, Kalkan) gizli | codex P1-05 | P1 | `Radial.cs:384-385,:400,:422-427`; `FrontendCopy.cs:210` | WP1 |
| 3 | Kilitli kule/uzmanlaşma ne zaman açılacağını söylemiyor; kilit rozeti 16 pt | claude P1-4 | P1 | `Radial.cs:163,:266-267,:326-338`; `TowerProgression.cs:29`; `FrontendCopy.cs:199` (`AfterLevel` kullanılmıyor) | WP1 |
| 4 | iPhone SE'de üst bar sıkışık, önizleme alanı dalga paneliyle ~6 pt çakışıyor (hesap) | claude P1-10 (kısmen) | P1 | `DefenderHudResponsiveLayout.cs:217-233` | WP1 (düzeltme) + WP11 (görüntü kanıtı) |
| 5 | Ara-dalga 8 sn otomatik başlatma geri sayımı görünmüyor | ikisi (cl P0-2, cx P1-06) | P0 | `DefenderHudController.cs:289` (`SetNextWaveLabel` boş); `Frame.cs:121` her kare eziyor; `GameSessionController.cs:55,:434-446`; `GameSessionController.Battle.cs:116-117` | WP2 |
| 6 | Sıradaki dalga içeriği yalnız "×50"; tür/sayı yok | ikisi (cl P1-6, cx P1-06) | P1 | `DefenderHudController.cs:278` (`SetPreview` boş); `Frame.cs:115`; `GameSessionController.cs:452-466` (önizleme metni zaten üretiliyor) | WP2 |
| 7 | Erken çağırmanın getirisi yok (`EarlyWaveReward => 0`) | ikisi (cl P1-5; cx P1-06 "bonus vaat etme") | P1 | `GameSessionController.Battle.cs:12`; `Frame.cs:118-121` | **Emre kararı** — varsayılan: WP2 bonus vaadi göstermez; onaylanırsa WP2 takibi |
| 8 | Hız adımları 1/2/4/8x → 1x↔2x önerisi | claude (WP2 notu) | P2 | `GameSessionController.cs:57-58` | **Emre kararı** — onaylanırsa WP2 takibi |
| 9 | Pause'dan YENİDEN BAŞLAT tek dokunuşla checkpoint'i siliyor (haritaya dönüşte onay var) | codex P1-07 | P1 | `Frame.cs:66-69`; `GameSessionController.cs:1118` | WP2 |
| 10 | Kahraman dönüş süresi yetenek bekleme süresiyle aynı görünüyor | codex P2-01 | P2 | `Frame.cs:126`; `UiRadialCooldown.cs:26` | WP2 |
| 11 | HUD boss barı ve "kaleyi aştı" mesajı her bölgede "Bölge Lordu" | bilinen (ikisi) | P1 | `Frame.cs:153`; `GameSessionController.cs:1015`; spec `.claude/ui-specs/boss-name-hud.txt` | WP2 (HUD kısmı) |
| 12 | Brifing/düşman kartında boss adı sabit; boss rozeti taç değil | bilinen (ikisi) | P2 | `AppFlowController.Screens.cs:636,:641`; taç ikonu yok (`UiDesignSystemAssets.cs`) | WP7 |
| 13 | Pause'ta yalnız müzik/efekt; hareket/sarsıntı/titreşim için menüye dönmek gerekiyor | ikisi (cx P1-08, cl P2-9) | P1 | `DefenderPresentationAssetBuilder.cs:1543-1558`; `Frame.cs:70-79`; menü ayarları `Screens.cs:532-545` | WP5 |
| 14 | Savaş HUD düğmeleri (pause/hız/dalga/yetenek/"?") VoiceOver'da adsız | ikisi (cx P1-09 savaş, cl P1-9) | P1 | `DefenderPresentationAssetBuilder.cs:1475-1479,:1509`; `DefenderHudController.cs:384,:403-416`; `DefenderHudController.Tutorial.cs:44`; `UiAccessibleButton.cs:49` | WP5 |
| 15 | Bilgi metinleri (düşman kartı açıklaması) VoiceOver düğümü değil; modal odak/geri dönüş yok | codex P1-09 (ortak) | P1 | `UiKit.cs:48-60`; `UiAccessibleButton.cs:49`; `Screens.cs:698-703` | WP3 (altyapı) + WP7 (düşman kartına uygulama) |
| 16 | Savaş alanı yuvalarının tam VoiceOver oynanabilirliği | codex P1-09 notu, claude P1-9 notu | — | — | **Kapsam dışı**: iki rapor da v1.1 dışı dedi; ayrı tasarım işi |
| 17 | iOS sistem "azaltılmış hareket" ilk açılışa taşınmıyor | ikisi (cx P2-03, cl P2-2) | P2 | `FrontendPreferences.cs:38,:44`; `AppFlowController.cs:323` | WP3 |
| 18 | Ana menü kartındaki bölüm ≠ BAŞLAT'ın açtığı checkpoint; dalga bilgisi yok | ikisi (cx P1-01, cl P1-1) | P1 | `AppFlowController.cs:429-433,:623-630`; `Screens.cs:131`; `GameSessionController.cs:920` | WP4 |
| 19 | Kayıt hatası/kurtarma açıklamasız; profil çipi yalnız ✓/✗ simgesi | ikisi (cx P1-02, cl P2-7) | P1 | `AppFlowController.cs:620-622,:681`; `AppFlowController.ClosedTest.cs:32` (`SaveStatus()` çağrılmıyor); `CampaignProgressStore.cs:484` | WP4 |
| 20 | Zafer ekranı yıldız kuralını (%75/%40 can) anlatmıyor; yıldız dolumunda ses/titreşim yok | ikisi (cx P2-02 sonuç, cl P1-7) | P1 | `AppFlowController.cs:644-648,:654-705,:673` | WP4 |
| 21 | 150/150'de final hâlâ "EKSİK YILDIZLARA DÖN" diyor | codex P2-02 (final) | P2 | `AppFlowController.CampaignCompletion.cs:65,:111`; `CampaignProgressStore.cs:220` | WP8 |
| 22 | Bölge sonu ve final sade metin kartı; büyük/küçük harf karışık | claude P1-8 | P1 | `CampaignCompletion.cs:53-136`; `Builds/flow-0b/region-Turkish.png`, `final-Turkish.png` | WP8 |
| 23 | Açılış/hesap/ilk zafer hesap bağlama vaadi (giriş "sonraya" kararıyla çelişiyor) | codex P1-03 | P1 | `Screens.cs:351-354,:407-412`; `AppFlowController.cs:700`; `FrontendCopy.cs:94` | **Emre kararı (2026-10-15): düğmeler GİZLENMEZ** → gizleme kapsam dışı; doğrulama WP12 |
| 24 | Apple/Google girişi fiziksel cihazda denenmedi; Apple token revoke yok | claude P0-3 | P0 | `Screens.cs:351-355,:407-409`; `TODO.md:11-15`; köprü `Assets/Plugins/iOS/AuthBridge.mm`, `Assets/Scripts/Core/IosExternalSignInBridge.cs` | WP12 (koordinatör + Emre); revoke = **Emre kararı** (Cloud Code + .p8, deploy) |
| 25 | Kilitli bölge metninde Türkçe ek hatası ("10'i/20'i/30'i/40'i") | claude P1-2 | P1 | `FrontendCopy.cs:113`; `CampaignCompletion.cs:33-34` | WP6 |
| 26 | Ondalık ayırıcı oyun diline değil cihaz kültürüne bağlı ("3,6/s") | ikisi (cx P2-04, cl P2-1) | P2 | `FrontendCopy.cs:10,:229-235,:376` | WP6 |
| 27 | EN terim tutarsızlığı (Level/Chapter, Defence/SPECIALIZE) | claude P2-5 | P2 | `FrontendCopy.cs` (`NextLevel` ↔ `UnlockedChapter`/`RegionLocked`) | WP6 |
| 28 | "Greenwatch" 5 bölgelik kampanyada her yerde (boot, varsayılan ad, zafer/yenilgi) | claude P2-6 | P2 | `FrontendCopy.cs:74,:89,:90,:255,:319,:362-363`; `GameLocalization.cs:150-151`; `GameSessionController.cs:1045` (literal) | WP6 (+WP2: `:1045` literal → `FrontendCopy.HudVictory`) |
| 29 | Öldürme altını popup'ları sürüde "+2+2+2" sütunu olup halkayı kapatıyor | claude P1-3 | P1 | `DefenderRewardPopupPool.cs:11`; `Builds/step6-after-chapter10-boss-1560x720.png` | WP9 |
| 30 | Boss girişi ve yenilgide titreşim yok | claude P2-3 | P2 | `DefenderPresentationDirector.cs:51-54` (kaçak → `UiError` → `UiHaptics.Error` zaten var, `GameSessionController.cs:1017`; eksik: `BossPhase`, `Defeat`) | WP9 |
| 31 | Brifing düşman çipleri yatay kayıyor ama belli değil; SE'de çok türde taşma kabulü yok | ikisi (cl P2-4, cx akış tablosu) | P2 | `Screens.cs:205-216`; `Builds/relearn/briefing-card-Turkish.png` | WP7 |
| 32 | Ayarlar: sürüm yok, dil "TR/EN", kaydırma ipucu yok, kapat düğmesi taşıyor | claude P2-8 | P2 | `Screens.cs:495-550` (`:545` "TR","EN" literal) | WP7 |
| 33 | Harita: kilitli düğümde bölüm no yok, kaydırınca bölge başlığı kayboluyor, yıldız satırı kırpılıyor | claude P2-10 | P2 | `Screens.cs:150-160,:290-296,:325-328`; `FrontendCampaignPager.cs`; `Builds/FrontendExperience/iOS/map-last-node-English.png` | WP7 (7b) |
| 34 | Hesap ekranında birbirine benzeyen 3 "bağla" düğmesi | claude P2-10 | P2 | `Screens.cs:407-412` | WP7 (gizlemeden ayrıştırma; #23 kararı) |
| 35 | SE / Pro Max / iPad görüntü kanıtı yok; `new-enemy.png` yanlış ekranı yakalıyor; kilitli bölge/150 yıldız/ölüm durumları görüntüsüz | ikisi (cl P1-10, cx İP6) | P1 | `ProjectSettings/ProjectSettings.asset:12` (`targetDevice: 2`); `AppFlowController.Acceptance.cs:133` | WP11 (+ **Emre kararı**: iPad desteği) |
| 36 | Savaş hazırlığı başarısızsa yalnız "tekrar dene"; geri dönüş yok | codex (akış tablosu) | P2 | `AppFlowController.cs:768,:828` | WP4 (opsiyonel, ≤15 dk) |
| 37 | Menü müziği yok | bilinen (ikisi) | P1 | `AppFlowController.cs:138` (UI ses kaynağı var, müzik yok) | **Emre kararı** — onaylanırsa WP4 takibi |
| 38 | BattlefieldLookTests biome SetPass 88 > 87 (+2 sınırı, tutarlı +3) | bilinen (koordinatör) | P1 | `Assets/Tests/PlayMode/BattlefieldLookTests.cs:214`; `.claude/handoff-latest.md:16` | WP10 |
| 39 | "İlk dalgayı başlat" düğmesi TestFlight'ta çalışmıyor | bilinen (Emre) | P0 | `AppFlowController.cs:792`, `Screens.cs:674,:692` (worker kök sebebi) | **Sürüyor**: ctx_a10942492ad8 (plan dışı; dosyaları WP4/WP7'yi bekletir) |
| 40 | Haptik, ses, VoiceOver, azaltılmış hareket, FPS fiziksel cihazda gözlenmedi | ikisi (kanıt sınırı) + `TODO.md:21-24` | P1 | — | WP12 (aynı cihaz oturumunda kontrol listesi) |

Kayıp kontrolü: Codex P1-01…09 → #18,19,23,1,2,5/6/7,9,13,14/15/16 · P2-01…04 → #10,20/21,17,26. Claude P0-1…3 → #1,5,24 · P1-1…10 → #18,25,29,3,7,6,20,22,14,35/4 · P2-1…10 → #26,17,30,31,27,28,19,32,13,33/34. Bilinenler → #11,12,37,38,39. Codex akış tablosu notları → #31,36. Eksik yok.

## 2. Paketler ve dosya sahipliği

Tüm yollar `Assets/` altında. "yeni" = paket oluşturur. Her paket yalnız kendi satırındaki dosyalara yazar; kesişim yok (aşağıdaki kontrol).

| Paket | Bulgular | Ownership | Süre | Kademe |
|---|---|---|---|---|
| **WP1 bilgi-karti** | #1 #2 #3 #4 | `Scripts/Presentation/DefenderHudController.Radial.cs`, `…Specializations.cs`, `…BattleChrome.cs`, `Scripts/Presentation/DefenderHudResponsiveLayout.cs`; yeni `Scripts/UI/Copy/FrontendCopy.BattleCard.cs`, `Tests/PlayMode/BattleInfoCardReadabilityTests.cs`; test güncelleme `Tests/PlayMode/BattleRadialInteractionTests.cs`, `BattleUiPolishTests.cs` | ~3 sa | K3 Claude opus high (maket `A-battle-combat.png` yargısı) |
| **WP2 dalga-akisi** | #5 #6 #9 #10 #11 (+#7/#8 karar gelirse; #28 `:1045`) | `Scripts/Presentation/DefenderHudController.cs`, `…Frame.cs`; yeni `…WaveForecast.cs`; `Scripts/UI/Kit/UiRadialCooldown.cs`; `Scripts/Core/GameSessionController.cs`, `GameSessionController.Battle.cs`; `Scripts/Data/FirstRegionCampaign.cs`, `CampaignCatalog.cs`; yeni `Scripts/UI/Copy/FrontendCopy.Wave.cs`, `Tests/EditMode/BossNameTests.cs`, `Tests/PlayMode/BattleWaveFlowTests.cs`; test güncelleme `Tests/PlayMode/BattleHudFrameTests.cs` | ~3 sa | K2 Codex sol medium |
| **WP3 a11y-altyapi** | #15 (altyapı) #17 | `Scripts/UI/Kit/UiKit.cs`, `UiAccessibleButton.cs`, `UiOverlay.cs`; yeni `Scripts/UI/Kit/UiAccessibleText.cs`; `Scripts/UI/FrontendPreferences.cs`; yeni `Scripts/UI/UiSystemAccessibility.cs`, `Plugins/iOS/UiSystemAccessibility.mm`, `Tests/EditMode/UiAccessibilityInfraTests.cs`; test güncelleme `Tests/EditMode/UiKitTests.cs` | ~2.5 sa | K2 Codex sol medium |
| **WP4 menu-devam-sonuc** | #18 #19 #20 #36 (+#37 karar gelirse) | `Scripts/UI/AppFlowController.cs`, `AppFlowController.ClosedTest.cs`; yeni `Scripts/UI/Copy/FrontendCopy.Session.cs`, `Tests/PlayMode/MenuContinueAndResultTests.cs`; test güncelleme `Tests/PlayMode/CampaignFlowTests.cs` | ~2.5 sa | K2 Codex sol medium |
| **WP5 hud-pause-voiceover** | #13 #14 | `Editor/DefenderPresentationAssetBuilder.cs`, `Art/GeneratedPresentation/DefenderHud.prefab` (yalnız builder regen), `Scripts/Presentation/DefenderHudController.Tutorial.cs`; yeni `Scripts/Presentation/HudPauseSettings.cs`, `HudBattleAccessibility.cs`, `Scripts/UI/Copy/FrontendCopy.HudAccess.cs`, `Tests/PlayMode/HudPauseAndVoiceOverTests.cs`; test güncelleme `Tests/PlayMode/AccessibilityCoverageTests.cs` | ~3 sa | K2 Codex sol medium (prefab regen riski yüksekse K3 astra high) |
| **WP6 metin-yerel** | #25 #26 #27 #28 | `Scripts/UI/FrontendCopy.cs`, `Scripts/UI/GameLocalization.cs`; yeni `Tests/EditMode/CopyConsistencyTests.cs`; test güncelleme `Tests/EditMode/CampaignLocalizationTests.cs`, `UiInfrastructureTests.cs`, `Tests/PlayMode/FrontendCopyBindingTests.cs` | ~1.5 sa | K2 Codex sol medium |
| **WP7 ekranlar** (7a → 7b, aynı worker) | #12 #15 (uygulama) #31 #32 #34 · 7b: #33 | `Scripts/UI/AppFlowController.Screens.cs`, `Scripts/UI/FrontendCampaignPager.cs`, `FrontendCampaignPageFitter.cs`, `FrontendRegionScrollRect.cs`, `Scripts/UI/UiDesignSystemAssets.cs`, `Editor/UiDesignSystemAssetBuilder.cs`, üretilen `Art/GeneratedPresentation/UiDesignSystemAssets.asset` + `UiPrimitives/*` + `Art/UI/AllinDefenseUI.spriteatlas` (yalnız builder); yeni `Scripts/UI/Copy/FrontendCopy.Screens.cs`, `Tests/PlayMode/FrontendScreensPolishTests.cs`; test güncelleme `Tests/PlayMode/RelearnExperienceTests.cs` | 7a ~2 sa + 7b ~1 sa | K2 Codex sol medium |
| **WP8 bolge-final** | #21 #22 | `Scripts/UI/AppFlowController.CampaignCompletion.cs`; yeni `Scripts/UI/Copy/FrontendCopy.Completion.cs`, `Tests/PlayMode/RegionCompletionPolishTests.cs` | ~2 sa | K3 Claude opus high (ödül anı görsel yargısı) |
| **WP9 popup-haptik** | #29 #30 | `Scripts/Presentation/DefenderRewardPopupPool.cs`, `DefenderPresentationDirector.cs`; test güncelleme `Tests/PlayMode/RewardPopupTests.cs`, `Tests/EditMode/UiHapticsTests.cs` | ~1.5 sa | K2 Codex sol medium |
| **WP10 biome-setpass** | #38 | `Scripts/Presentation/BiomeLook.cs`, `DefenderBattlefieldDressing.cs`, `DefenderSceneLook.cs`; `Tests/PlayMode/BattlefieldLookTests.cs` (sınır yalnız onayla), `Tests/EditMode/BiomeLookTests.cs` | ~1.5 sa | K3 Codex astra high (kök sebep) |
| **WP11 gorsel-kabul** | #35 #4 (kanıt) | `Scripts/UI/AppFlowController.Acceptance.cs`, `Editor/FrontendExperienceReview.cs`, `Tests/PlayMode/FrontendExperienceTests.cs`; yeni `Tests/PlayMode/UiUxV11AcceptanceTests.cs`, `docs/audit/2026-10-15/DEVICE_ACCEPTANCE.md`; çıktı `Builds/uiux-v11/**` | ~2 sa + koordinatör grafikli koşu ~30 dk | K2 Codex sol medium |
| **WP12 iphone-giris** | #24 #40 (#23 doğrulaması) | Kod yok (koordinatör + Emre). Hata çıkarsa ayrı kod spec'i yazılır (`IosExternalSignInBridge.cs`, `AuthBridge.mm`, `ExternalSignIn.cs`, `UnityGameServicesController.cs`) | ~45 dk cihazda | Koordinatör + Emre |

**Kesişim kontrolü (elle, dosya bazında):**
- `DefenderHudController` partial'ları: Radial/Specializations/BattleChrome → WP1 · ana dosya/Frame/WaveForecast → WP2 · Tutorial → WP5 · Motion/Sales → kimse.
- `AppFlowController` partial'ları: ana/ClosedTest → WP4 · Screens → WP7 · CampaignCompletion → WP8 · Acceptance → WP11 · Hero/AccountDeletion → kimse.
- `GameSessionController.*` → yalnız WP2. `UiKit/UiAccessibleButton/UiOverlay` → WP3, `UiRadialCooldown` → WP2.
- `FrontendCopy.cs` → yalnız WP6; partial `Copy/FrontendCopy.<X>.cs` dosyaları her pakette farklı ad. Builder'lar: HUD builder → WP5, UI design system builder → WP7.
- Test dosyaları tek sahipli (tabloda); `FirstWaveButtonTests.cs` hiçbir pakette yok (ilk dalga worker'ının).
- Sonuç: kesişim boş.

## 3. Dalga sırası ve bağımlılıklar

| Dalga | Paketler (paralel) | Başlama şartı | Bağımlılık notu |
|---|---|---|---|
| **W0** | koordinatör: `FrontendCopy` partial (1 satır) | şimdi | W1'deki partial copy dosyaları buna bağlı |
| **W1** | WP1 · WP2 · WP3 | W0 | İlk dalga worker'ının dosyalarına (`AppFlowController.cs`, `…Screens.cs`, `FirstWaveButtonTests.cs`) dokunmazlar. WP3 `UiKit` API'sini yalnız **ekleyerek** değiştirir (WP1/WP2 aynı API'yi kullanıyor). |
| **W2** | WP4 · WP5 · WP6 | W1 merge + ilk dalga worker'ı (ctx_a10942492ad8) commit'lendi | WP4 ← ilk dalga düzeltmesi (`AppFlowController.cs`). WP5 ← WP2 (Frame/HUD etiket durumları, geri sayım) + WP3 (a11y değer API'si). WP6 ← WP2 (`GameSessionController.cs:1045` literal'i kaldırıldı). |
| **W3** | WP7 (7a→7b) · WP8 · WP9 | W2 merge | WP7 ← ilk dalga düzeltmesi (`Screens.cs`) + WP2 (boss adı erişimcisi) + WP3 (`UiAccessibleText`). WP8, WP9 bağımsız (kapasite yüzünden W3). |
| **W4** | WP10 · WP11 | W3 merge | İkisi de grafikli koşu ister → koordinatör sırayla tek oturumda koşar, ardından tam grafikli PlayMode bir kez. |
| **W5** | WP12 (koordinatör + Emre) | TestFlight build 2 yüklendi | Fiziksel iPhone; hata çıkarsa yeni kod spec'i. |

Kritik yol: W0 → W1 (~3 sa) → W2 (~3 sa) → W3 (~3 sa) → W4 (~2.5 sa) → build 2 → W5. Toplam worker süresi ~26 sa, takvimde ~12 sa.

## 4. Emre kararları (açık)

1. **Menü müziği (#37)** — eklensin mi? Evetse müzik dosyası hangisi? (WP4 takibi, ~45 dk.)
2. **Erken çağırma bonusu (#7)** — kalan süreye orantılı altın bonusu eklensin mi? Denge testlerini etkiler. Varsayılan: yok, vaat yok.
3. **Hız adımları (#8)** — 1/2/4/8x kalsın mı, 1x↔2x mi?
4. **iPad (#35)** — iPad desteği sürsün mü (iPad ekran görüntüsü + kabul matrisi), yoksa yalnız iPhone (`targetDevice: 0`)?
5. **Apple token revoke (#24)** — App Store hesap silme kuralı Apple girişli hesapta token iptali istiyor; Cloud Code + Apple `.p8` anahtarı + deploy gerekir. v1.1'e girsin mi?
6. **Biome SetPass (#38)** — WP10 kök sebep bulamazsa sınır +3'e çekilsin mi?
