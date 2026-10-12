# Adım 4a cila — HUD çerçevesi

12 Ekim 2026; yalnız `/Users/emreaydin/AllinDefense-ui`, `ui-modernization`; başlangıç `2e7b99b`. Değişiklikler commit edilmedi.

## Değişiklikler

- `Assets/Scripts/UI/Kit/UiPillShape.cs:8`: Sliced pill köşe ölçeği kısa kenara göre hesaplanır. Önceki border değerleri pozitifti; esas elips nedeni iki eksende farklı border sıkışmasıydı. `UiKit.cs:80` dekorasyonlara, `:159` pill IconButton'a ve `:240` segmentlere aynı kit davranışı uygulanır; dairesel IconButton Simple kullanır.
- `Assets/Scripts/UI/Kit/UiKit.cs:85`, `UiProgressSegments.cs:15` ve `DefenderHudController.Frame.cs:139`: yatay ilerleme/can oranı Sliced görüntünün anchor genişliğiyle sunulur; Filled tipiyle şeklin uzaması önlenir. Mevcut `fillAmount` gözlem değeri korunur.
- `Assets/Editor/UiDesignSystemAssetBuilder.cs:73`: GoldGradient 16 px border, yuvarlatılmış ve antialias edilmiş alpha ile üretilir. `UiKit.cs:118`: altın CTA gradient'i sert stencil Mask yerine kendi köşe alpha'sını kullanır; bilinear filtre, mipmap kapalı ve uncompressed import korunur.
- `Assets/Editor/DefenderPresentationAssetBuilder.cs:1379`: portre, FilledCircle sprite'lı Mask içinde kırpılır. `DefenderHudResponsiveLayout.cs:253`: yetenek rozet/halka bütünü yukarı taşınır; kahraman etiketi halkanın altında ortalanır.
- `DefenderPresentationAssetBuilder.cs:1365`, `DefenderHudResponsiveLayout.cs:250` ve `DefenderHudController.cs:420`: MessageChrome kit dekorasyonu koyu zemin sağlar; metin boşken zemin de kapanır. Mevcut iki satırlı metin alanı korunur; rota kapatılmaz.
- `Assets/Scripts/UI/AppFlowController.Acceptance.cs:106`: EN hazırlık çekimi korunur, TR çekimi eklenir; kabul aracında öğretici mesajı dil değişiminde yeniden üretilip EN akışına dönülür. Normal oyun davranışı değiştirilmedi.
- `Assets/Tests/EditMode/UiKitTests.cs:66`: yeni EditMode testi pill/rounded Image'ların Sliced ve border pozitif olmasını, 44 pt pill köşelerinin eşit ölçeğini doğrular; mevcut segment testi yeni Sliced sunumla aynı dolum oranlarını denetler.

## Kapılar

- EditMode: **203/203 geçti**, 0 başarısız; `Builds/adim4a-polish-edit.xml` (son kaynak, 20:27 UTC).
- Son tam PlayMode: **135/135 geçti**, 0 başarısız; `Builds/adim4a-polish-play-final.xml`, 20:28:16–20:36:54 UTC, 517,52 sn. Önceki tam koşu da 135/135 yeşil: `Builds/adim4a-polish-play.xml`, 530,41 sn.
- HUD prefab yeniden üretildi: `Builds/adim4a-polish-hud-regen.log`; GUID korundu: `d0eb9a43eadfb4d768c166ba3b9a6beb`.
- Vitrinler yeniden üretildi ve yatay/dikey görsel kontrol yapıldı: `Builds/ui-kit-showcase.png`, `Builds/ui-kit-showcase-portrait.png`; log `Builds/adim4a-polish-showcase.log`.
- Simülatör: Unity ve Xcode derlemeleri başarılı; `tools/build_and_run_ios_simulator.sh` ve `tools/run_frontend_experience_simulator.sh` çalıştırıldı. `Builds/FrontendExperience/iOS/report.json`: **status=passed, errors=[]**, 21 ekran görüntüsü; 6 mevcut 4b `knownIssues` kaydı.

- `git diff --check` temiz; `ProjectSettings.asset`, iOS Addressables content state ve NotoSans-Bold-DC font build yan etkileri geri alındı. Her batch sonrası `link.xml` ve meta dosyası geri yüklendi.

Önceki FrameScreenRects alan ölçümü korunmuştur; kahraman etiketi 92→82 pt daraldığı için oranlar hafif azalmıştır. Gölge/kenar payı alanları bu ölçümün ayrı kalemleri değildir.

| Çözünürlük | Önceki HUD | Yeni HUD | Rota | Yuva | Kale | HUD kesişimi |
|---|---:|---:|---:|---:|---|---:|
| 800×450 | %16,34 | %16,29 | 1 | 8 | Görünür | 0 |
| 844×390 | %17,87 | %17,82 | 1 | 8 | Görünür | 0 |
| 1024×768 | %7,48 | %7,46 | 1 | 8 | Görünür | 0 |

## Güncel görüntüler

- `Builds/FrontendExperience/iOS/battle.png` — EN hazırlık.
- `Builds/FrontendExperience/iOS/battle-Turkish.png` — TR hazırlık; DALGA/HAZIRLIK/HAZIR, CTA, kahraman ve öğretici mesajı taşmasız.
- `Builds/FrontendExperience/iOS/battle-active-wave.png` — EN aktif dalga ve Sliced kısmi dolum.
- `Builds/FrontendExperience/iOS/battle-level-10-boss.png` — gerçek canlı boss, can dolumu ve iki satırlı yetenek metni.
- `Builds/FrontendExperience/iOS/pause.png` — yumuşak köşeli Resume.
- `Builds/FrontendExperience/iOS/pause-map-confirmation.png` — haritaya dönüş onayı.
- `Builds/HudFrame/preparation-800x450.png`, `preparation-844x390.png`, `preparation-1024x768.png` — oran testi görüntüleri.

## Kalan kapsam

SelectedPanel, inşa, seçili kule, uzmanlaşma ve öğretici okuna dokunulmadı; pause arkasındaki mevcut taşma 4b'ye aittir. Menü ekranlarına özgü veya savaş alanında değişiklik yapılmadı; kabul allowlist'i genişletilmedi.
