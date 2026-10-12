# ALLIN: Defense — taze kod envanteri (12 Ekim 2026)
Normal oyuncu akışı 10 bölüm/1 bölge açıyor; 50 bölüm/5 bölge katalogda bulunuyor (AppFlowController.ClosedTest.cs:12; CampaignCatalog.cs:45).
Üretim savaşında 3 kule türü, 6 uzmanlaşma, 6 düşman arketipi ve 1 sabit kahraman var (GreyboxDefinitions.cs:6; TowerProgression.cs:8; GameSessionController.cs:806).
Yıldız, Valor ve heroXP kazanılıyor; Valor/XP harcaması veya kahraman seviyesine etkisi bulunamadı (CampaignProgressStore.cs:433; aşağıdaki M2 araması).
Gizlilik/koşul URL’leri boş, hesap silme uygulaması bulunamadı; ürün ayarları kapalı test kimliği taşıyor (FrontendPresentationTheme.asset:41; ProjectSettings.asset:16,169; M3).
Mevcut en yeni testler 8 Eylül tarihli dar kapsamlı testler; bugünkü çalışma ağacının derlenmesi ve cihaz davranışı Doğrulanamadı (XML:1).

## Yöntem ve sınırlar
- Repo salt okundu; yalnız bu rapor yazıldı. Unity, test/build, UGS/Cloud Code ve git yazma komutları çalıştırılmadı.
- Eski audit/TODO belgeleri dayanak alınmadı; ekran görüntüsü kullanılmadı. “Gösteriliyor” kodun UI’a bağladığı metni ifade eder; ekrandaki görünürlük/taşma Doğrulanamadı.
- Aşağıdaki kısa dosyalar: Core=Assets/Scripts/Core/, Data=Assets/Scripts/Data/, UI=Assets/Scripts/UI/, Presentation=Assets/Scripts/Presentation/; BattleRules.cs=Assets/Scripts/Combat/BattleRules.cs; FirstRegionCampaign.json=Assets/Resources/FirstRegionCampaign.json; tema/katalog .asset dosyaları=Assets/Resources/Presentation/; diğer yollar açık yazıldı.
- Sayılar Python pathlib ile mevcut dosyaların satırlarından/JSON/XML’lerinden alındı; XML tarihleri test-run start-time ve dosya mtime üzerinden karşılaştırıldı.
- M1: `rg -n 'heroXp|valor' Assets/Scripts --glob '*.cs'`; M2: `rg -n 'heroXp|valor|HeroXp|Valor|Spend' Assets/Scripts CampaignProgressionCCM/CampaignProgression --glob '*.cs'`.
- M3: `rg -n 'DeleteAccount|DeletePlayer|consent|Consent|Analytics|Sentry|Crashlytics' Assets/Scripts CampaignProgressionCCM/CampaignProgression Packages/manifest.json` → eşleşme yok.

## 1. Oynanış içeriği
### Kuleler — normal savaş yolu
| Tip | Açılış | Sv1 kurulum / Sv2 yükseltme / Sv3 uzmanlaşma (altın) | Sv1 hasar, menzil, hız | Sv3 seçenekleri |
|---|---|---|---|---|
| Okçu | Başlangıç | 75 / 55 / 160 | 24; 4,8; 2,5 atış/sn | Hızlı Atış / Keskin Nişancı; bölüm 6 açılması |
| Kışla | Bölüm 2 açılması | 100 / 70 / 180 | Asker başına 16; 0,85; 2,667/sn; 3 asker, 110 HP | Kalkan / Hücum Birliği; bölüm 8 açılması |
| Büyücü | Bölüm 3 açılması | 125 / 85 / 220 | 38; 4,2; 1,3/sn; 1,65 alan yarıçapı | Patlama / Buz; bölüm 7 açılması |
- Kanıt: TowerProgression.cs:21,28,30,32,41,53; GreyboxCombatants.cs:443,454; GameSessionController.Battle.cs:21. “Açılış” kontrolü mevcut oynanan bölümden çok en yüksek açılmış bölüme bakıyor (Battle.cs:16).
- Sv2: Okçu hasar ×1,75, menzil ×1,12, hız ×1,44; Büyücü hasar ×1,55, menzil ×1,12, hız ×1,2; Kışla askerleri 32 hasar/210 HP (TowerProgression.cs:46,53).
- Sv3: Hızlı Atış hasar ×0,7/hız ×1,8; Keskin Nişancı hasar ×1,8/hız ×0,72/menzil ×1,3 ve şifacı/kuşatma önceliği; Kalkan HP ×1,8; Hücum hasar ×1,55/hız ×1,15; Patlama hasar ×1,2/hız ×0,9/alan ×1,45; Buz hasar ×0,7 ve 2 sn %50 yavaşlatma (TowerProgression.cs:64; Battle.cs:37).
- Katsayılar Sv2 üzerinden uygulanır; hasar yuvarlanır. DPS (saniyedeki hasar) teorik olarak Okçu 60, Büyücü 49,4, Kışla asker başına 42,7; hedef bulma, zırh, alan ve asker kaybı nedeniyle gerçek DPS değildir (TowerProgression.cs:41).
- Ayrı eski kule yolu ArcherTower/WarriorTower/MageTower/CatapultTower var; normal oturum GreyboxOutpost kullanıyor, Catapult normal oyuncu seçimi değil (GameSessionController.cs:169; GreyboxDefinitions.cs:6; Assets/Scripts/Towers/*.cs).

### Düşmanlar ve boss
| Arketip | Taban HP | İlk bölgede hız | Kaleye zarar | İlk bölge öldürme altını | Özellik |
|---|---:|---:|---:|---:|---|
| Yağmacı | 75 | 4,5 | 1 | 2 | Hızlı temel düşman |
| Öncü | 230 | 2,7 | 1 | 5 | %50 fiziksel direnç; büyü direnci yok |
| Yenilenen | 420 | 1,9 | 2 | 8 | 0,625 sn’de max(5,HP/25) iyileşme |
| Şifacı | 180 | 2,5 | 1 | 6 | 1,2 sn’de 3,5 menzilde diğer düşmanlara 24 iyileşme |
| Kuşatma | 620 | 1,44 | 3 | 12 | 1,2 sn uyarıdan sonra kuleyi 1,5 sn kapatma; 3 sn çevrim |
| Bölge Lordu | 2600 | 1,24 | 5 | 50 | Kuleyi 2,5 sn kapatma; 4 sn çevrim; yavaşlatma en çok %25 |
- Kanıt: GameSessionController.cs:2722,2733; FirstRegionCampaign.json:9 (killRewards); GreyboxCombatants.cs:121,149,225,248. HP bölüm healthMultiplier ile artar; tabloda taban HP verildi.
- Can %66’ya düşünce faz 2: 6 Yağmacı takviyesi; %33’te faz 3: hareket hızı ×1,44. Kuşatma uyarısı sersemletmeyle kesilebilir (GreyboxCombatants.cs:169,194,251; GameSessionController.Battle.cs:153).
- Katalogda her 10. bölüm boss olarak işaretli, normal akışta yalnız bölüm 10’un boss’u erişilebilir; farklı isimler ayrı boss davranışları anlamına gelmiyor (CampaignCatalog.cs:74,127; GreyboxCombatants.cs:169).

### Bölüm, harita, kahraman, meta
| Sistem | Bugünkü kod/veri envanteri | Kanıt |
|---|---|---|
| Normal kampanya | 1 bölge, 10 bölüm; yeni profil bunları sırayla açar | AppFlowController.ClosedTest.cs:12; CampaignProgressStore.cs:436 |
| Katalog | 5×10=50 bölüm; 11–50 için üretilen dalgalar var, normal ön yüzde kapalı | CampaignCatalog.cs:45; CampaignLevelFactory.cs:89,105 |
| İlk bölge dalgaları | 1–3: 3 dalga/246 düşman; 4–9: 6 dalga/567–616; 10: 8 dalga/824 (faz 2’nin 6 takviyesi hariç); hedef süre 2–3 / 4–6 / 6–8 dk | FirstRegionCampaign.json, `json.loads(...)["levels"]` üzerinden grup toplamı |
| Haritalar | 50 map-ID, 5 yol şablonundan kaydırma/ayna/dallanmayla üretilir; 50 elle çizilmiş harita değildir | CampaignCatalog.cs:128; CampaignLevelFactory.cs:45,172,189 |
| Biyomlar | 5 renk/ışık paleti; normal ön yüzde 1 biyom/10 map-ID | CampaignLevelFactory.cs:124,148,181; AppFlowController.ClosedTest.cs:12 |
| Giriş sayısı | İlk 10 tek giriş; 14+ iki, 43+ üç giriş katalog kodu | CampaignLevelFactory.cs:175 |
| Kahraman | 1 sabit kale savunucusu; isim değişir, kahraman seçimi yok; 240 HP, 20 hasar/0,35 sn, 1,65 saldırı menzili, 5 sn yeniden doğuş | GameSessionController.cs:784,806; GreyboxCombatants.cs:724; AppFlowController.Hero.cs:15 |
| Aktif yetenek | Sarsıcı Darbe: 3,2 yarıçap, 90 hasar, 1 sn sersemletme, 6 sn bekleme; menzilde hedef yoksa harcanmaz | GreyboxCombatants.cs:728,789 |
| Eski kahramanlar | Knight/Ranger/Wizard/Elven ve hareket komutu sınıfları var; güncel savaşta kullanılan kahraman değildir | Assets/Scripts/Heroes/*.cs; GameSessionController.cs:806; prototip taraması aşağıda |
| Yıldız | Zaferde kalan can ≥%75: 3; ≥%40: 2; diğer pozitif can: 1; en iyi skor saklanır | BattleRules.cs:13; CampaignProgressStore.cs:429 |
| Valor/heroXP | İlk zaferde katalog ödülü; iyileşen her yıldız +5 Valor; XP yalnız ilk zafer; tekrar ödül üretmez | CampaignProgressStore.cs:433; CampaignProgressionCCM/CampaignProgression/CampaignProgressionModule.cs:220 |
| Harcama | Valor/XP harcaması, kahraman seviyesine/statına etkisi bulunamadı; yıldız bölüm başarısı/açılış içindir | M1/M2; GreyboxCombatants.cs:724; CampaignProgressStore.cs:436 |
| Kayıt | Yerel profil, bekleyen tamamlama talepleri, açık misafir aktarımı ve sunucu eşitleme kodu var; canlı dağıtım Doğrulanamadı | CampaignProgressStore.cs:204,273; AppFlowController.ClosedTest.cs:27; UnityGameServicesController.cs:171 |
| Cloud Code | GetState/SubmitCompletionBatch; campaign_state_v3, rules-v2, en çok 20 talep/batch; v1/v2 JSON katalogları 50 bölüm | CampaignProgressionModule.cs:12,28,52,105; Assets/Resources/LiveGame/*.json |
| Öğretici | İlk bölüm/ilk zafer öncesi: kur, ikinci kule, dalga başlat, yetenek, üçüncü kule, yükselt ipuçları; sonraki bölümlerde öğrenme hedefi | GameSessionController.Tutorial.cs:15,24; AppFlowController.ClosedTest.cs:51 |
| Ses/müzik | Katalogda 22 efekt cue’su/6 müzik durumu ve gerçek clip referansları; crossfade, duraklatma, müzik kısma; üretilen rüzgâr/yaprak/kristal ambiyansı | DefenderPresentationCatalog.asset:137,247; DefenderAudioController.cs:200,233,266; DefenderAmbientAudioController.cs:157 |
| Dil | Türkçe/İngilizce; cihaz dilinden varsayılan, saklanan seçim; eksik çeviri anahtarı Türkçe’ye döner | GameLocalization.cs:11,126,138,149 |

## 2. UI’da korunacak mevcut veriler ve eksikler
| Veri/sistem | Kodda var mı? | Bugünkü UI durumu (statik kod kanıtı) |
|---|---|---|
| Hasar/menzil/atış hızı, seviye, yükseltme/satış | Evet: TowerProgression.cs:41; GreyboxCombatants.cs:347 | Gösteriliyor: DefenderHudController.cs:860,882; menzil zaten eksik değil |
| Birleşik DPS / yükseltme önce-sonra farkı | Hesaplanabilir; hazır DPS alanı yok: TowerProgression.cs:10,41 | Seçim panelinde DPS/fark gösterimi yok: DefenderHudController.cs:882 |
| Kışla asker HP/sayısı; alan yarıçapı; öncelikli hedef | Evet: TowerProgression.cs:53,64 | Rol cümlesi kısmen anlatır; sayısal HP/alan/önce-sonra yok: TowerProgression.cs:91; DefenderHudController.cs:882 |
| Düşman HP/hız/zırh/iyileşme/kaleye zarar | Evet: GameSessionController.cs:2722; GreyboxCombatants.cs:121,225 | İsimler ve bölüm öğrenme hedefi/yenilgi tavsiyesi var; ayrıntılı düşman sözlüğü yok: AppFlowController.cs:474,669; ClosedTest.cs:51 |
| Dalga önizlemesi | Evet: FirstRegionCampaign.cs:33; WaveDefinition.groups | Tür ve adet gösteriliyor: GameSessionController.cs:444,461; ayrıntılı zamanlama/rota/ödül gösterimi yok |
| Boss fazları | Evet: GreyboxCombatants.cs:169 | Bölüm hedefi %66/%33’ü açıklar, görsel faz sinyali var; sayısal faz mekanik kartı yok: ClosedTest.cs:62; GreyboxCombatants.cs:176 |
| Bölüm dalga/süre/başlangıç altını/giriş | Evet: CampaignCatalog.cs:119; CampaignLevelFactory.cs:175 | Gösteriliyor, taktik harita da var: AppFlowController.cs:465,471 |
| Ödül Valor/XP ve bakiyeler | Evet: CampaignCatalog.cs:125; CampaignProgressStore.cs:47,433 | Gizli: briefingReward artık HEDEF; sonuç reward argümanını göstermiyor, profil yalnız yıldız gösteriyor: AppFlowController.cs:475,605,647,664 |
| Yıldız/başarı, kayıt ve eşitleme | Evet: CampaignProgressStore.cs:59,204 | Harita/sonuç/profil ve kayıt durumu gösteriliyor: AppFlowController.cs:540,605,661; ClosedTest.cs:15 |
| Kahraman HP/yetenek gücü/respawn | Evet: GreyboxCombatants.cs:724 | Yetenek hazır/bekleme/dönüyor gösterimi var; kalıcı gelişim ağacı yok: DefenderHudController.cs:852; M2 |
| 11–50 içerik ve diğer biyomlar | Üretim kodu var: CampaignLevelFactory.cs:89 | Normal ön yüzde saklı: ClosedTest.cs:12; açmak için önce oynanış doğrulaması gerekli (çıkarım) |
| Hesap yönetimi / silme | Giriş/hesap değiştirme var; silme bulunamadı: M3 | Hesap ekranı giriş + yasal linkler; silme butonu yok: AppFlowController.Screens.cs:275,294,300 |
- UI tasarımı kararı (çıkarım): kazanılıp harcanamayan Valor/XP’yi “yükseltme para birimi” diye sunmadan önce ürün döngüsü kararlaştırılmalı; mevcut dalga/menzil/yıldız gösterimleri korunmalı.

## 3. Teknik sağlık
- Runtime C# dosya sayısı 124; GameSessionController*.cs toplamı **3513 satır** (boş satırlar dahil; Python `rglob` + `splitlines`). Ana dosya 3014 satır; test/capture kodları da içeriyor (GameSessionController.cs:1350,1413,2042).
- En büyük 10 C# (Assets + CampaignProgressionCCM, obj/bin hariç; kapsam farkı için runtime ilk 10 da aşağıda):
| Dosya | Satır |
|---|---:|
| Assets/Scripts/Core/GameSessionController.cs | 3014 |
| Assets/Editor/DefenderPresentationBuildValidator.cs | 2581 |
| Assets/Tests/EditMode/CoreRulesTests.cs | 1965 |
| Assets/Scripts/Presentation/DefenderHudController.cs | 1727 |
| Assets/Editor/DefenderPresentationAssetBuilder.cs | 1482 |
| Assets/Scripts/UI/AppFlowController.cs | 1425 |
| Assets/Tests/PlayMode/GreyboxSceneTests.cs | 1016 |
| Assets/Scripts/Core/GreyboxCombatants.cs | 860 |
| Assets/Scripts/Presentation/DefenderBattlefieldDressing.cs | 663 |
| Assets/Scripts/Data/CampaignLevelFactory.cs | 659 |
- Assets/Scripts runtime ilk 10: GameSessionController 3014; DefenderHudController 1727; AppFlowController 1425; GreyboxCombatants 860; DefenderBattlefieldDressing 663; CampaignLevelFactory 659; DefenderVfxPool 559; DefenderPresentationCatalog 483; CampaignProgressStore 454; StylizedPresentationFactory 448 (aynı sayım).
- Kullanılmıyor/prototip ADAYI: **14 sınıf/14 dosya**: Heroes/{KnightHero,RangerHero,WizardHero,ElvenHero,HeroCommandController}; Core/WaveManager; Utils/TouchInputHandler; Units/{MeleeUnit,RangedUnit,MagicUnit,UnitRespawnSpawner}; Enemies/{BossEnemy,SiegeEnemy,SupportEnemy}.
- Tespit: her `class` adını diğer Assets/Scripts/*.cs metninde sözcük sınırıyla aradım; bu 14 için dış script referansı yok. .cs.meta GUID’lerini Assets/**/*.unity,prefab,asset içinde aradım; referans yok. Kesin ölü kod hükmü değil: yansıma/dış editor kodu/elle ekleme Doğrulanamadı.
- Geniş aynı tarama 46 sınıfta dış runtime dosya referansı bulmadı; iç sınıflar, serileştirme veri sınıfları, editor tarafından kullanılan asset tipleri ve runtime başlangıç hook’ları da dahil olduğundan 46’yı “ölü sınıf” sayısı olarak kullanmadım.
- Eski Towers/Enemies/Grid/GameManager/PlayerProgress ailesi ayrıca ayrı oyun yolunun adayı; bazı prefab/script bağları bulunduğundan tamamını kullanılmıyor ilan etmedim (Assets/Resources/Prefabs/**; GameBootstrap.cs:28).
| Mevcut XML (en yeni ada göre kategorize edilen test-run) | Tarih | Toplam | Geçen | Başarısız | Atlanan |
|---|---|---:|---:|---:|---:|
| Builds/FrontendExperience/loading-feedback-editmode.xml:1 | 2026-09-08 | 6 | 6 | 0 | 0 |
| Builds/FrontendExperience/loading-feedback-playmode-final.xml:1 | 2026-09-08 | 7 | 7 | 0 | 0 |
| Builds/beautification-editmode.xml:1 | 2026-08-27 | 78 | 78 | 0 | 0 |
| Builds/beautification-playmode.xml:1 | 2026-08-27 | 13 | 13 | 0 | 0 |
- En yeni iki dosyanın birleşik sayısı 13/13/0; bu tüm oyun süitinin sonucu değildir. Daha önce loading-feedback-playmode-rerun.xml:1 = 7 toplam/6 geçen/1 başarısız; final dosyada yeşil. XML’ler Ekim rename’ini doğrulamaz.
- İstenen örnek merged-editmode2.xml/merged-playmode.xml mevcut Builds ağacında bulunmadı (`Path("Builds").rglob("merged-*.xml")`). Yeni test çalıştırılmadı; bugün derlenebilirlik Doğrulanamadı.
- Android: Apply ARM64/IL2CPP, min SDK29/auto target; development APK ve release AAB metotları (Assets/Editor/AndroidBuildSettings.cs:17,35,44).
- iOS: cihaz development Xcode projesi + simulator development/release; iOS15+, iPhone+iPad (Assets/Editor/IosFirstBuildSettings.cs:36,43,48,98). Cihaz App Store archive/upload yolu kodda bu scriptte yok.
- Kapalı test: iki platformda .closedtest, 0.4.0, development/debugging; ct-20260906-01 (Assets/Editor/ClosedTestBuilds.cs:13,20,34; BattleTelemetry.cs:8).
- tools/: cihaz/simulator build-run, slot/soak/ratio/performance/audio/actor/vis812 smoke, görsel/brand doğrulayıcıları mevcut; çalıştırılmadı (`rg --files tools`). Fiziksel imza kontrolü ayrı Python aracı var: tools/check_ios_physical_readiness.py.

## 4. Yayın engelleri — kod/ayar temelli sıralama (hukuki uygunluk değerlendirmesi değildir)
1. **Yasal linkler boş:** FrontendPresentationTheme.asset:41–42; butonlar bu alanları açıyor (AppFlowController.Screens.cs:300). Canlı URL ve açılma davranışı Doğrulanamadı.
2. **Hesap silme akışı bulunamadı:** M3; gerçek Player Accounts girişi var (UnityGameServicesController.cs:137,175), hesap ekranı yalnız giriş/yasal linkler içeriyor (Screens.cs:294). Silme ve sunucu verisi temizliği ürün kararı/uygulama gerektiriyor (çıkarım).
3. **Yayın kimliği seçilmemiş:** PlayerSettings ürün adı ALLIN: Defense Test, Android+iOS com.emreaydin.allindefense.closedtest; v0.4.0, Android versionCode1 (ProjectSettings.asset:16,146,169,177). Release Apply metotları com.emreaydin.allindefense yazıyor; yanlış build giriş noktası test kimliği üretir (AndroidBuildSettings.cs:14; IosFirstBuildSettings.cs:11; ClosedTestBuilds.cs:36).
4. **İmza/provisioning hazır olduğu kanıtlanmadı:** Apple team/profile boş, automatic signing0; Android keystore/alias boş (ProjectSettings.asset:249,250,256,276). Cihaz aracı DC_IOS_TEAM_ID/yerel development identity keşfi ve Automatic signing kullanır; App Store dağıtım imzasının kanıtı değildir (tools/build_and_run_ios_device.sh:41,95,112).
5. **Analitik/crash/onay kapsamı eksik:** BattleTelemetry yerel JSONL tutar; RuntimeLogCapture yerel log kodudur (BattleTelemetry.cs:33,46; RuntimeLogCapture.cs:1). Analytics/Sentry/Crashlytics/consent çağrısı bulunmadı (M3); manifestte services analytics/crash paketi yok, unityanalytics motor modülü bulunması entegrasyon kanıtı değil (Packages/manifest.json:14,48). SDK toplama/mağaza beyanları ve cihaz davranışı Doğrulanamadı.
6. **İkon durumu:** iOS tüm ilgili slotlar BrandV2 GUID’ine bağlı (ProjectSettings.asset:305; Assets/Art/AppIcon/BrandV2/AllinDefense-AppIcon-v2-1024.png.meta:2). Android kayıtlı platform slotlarında boş texture’lar var (ProjectSettings.asset:419); Apply ve build preprocessor ikon atıyor (MobilePortraitBuildSettings.cs:14,47). Android çıkan APK/AAB ikonu Doğrulanamadı; “ikon yok” diye kesin hüküm verilemez.

## 5. Ad değişikliği kalıntıları
- Tam istenen tarama yapıldı; kanıt komutu: `grep -rIn -E "DefenderChronicles|Defender Chronicles|defender-chronicles|defenderchronicles|DEFENDER CHRONICLES" --exclude-dir=Library --exclude-dir=Temp --exclude-dir=Logs --exclude-dir=Builds --exclude-dir=build --exclude-dir=.git .`.
- Sonuç: **4439 metin eşleşmesi / 104 dosya**; eski audit belgelerinden durum çıkarılmadı, yalnız ad eşleşme envanteri alındı. Aşağıdaki listede her kalan dosya yer alır; klasör başlığı altındaki adlar o klasöre göredir.
- Etkin ayar: ProjectSettings/ProjectSettings.asset:990 `projectName: Defender Chronicles`; ürün adı/bundle ID ayrı ve yenilenmiş. Cloud project görünen adı kalıntısı; kendi başına derleme/ID kırılması kanıtı değil (çıkarım).
- Eski geçici test sahnesi: Assets/InitTestScene759a220b-38e9-48c8-8a9b-14778e39ada0.unity:152,173 eski PlayModeTests assembly ve test sınıfı seçiyor. Bu sahneyle test yeniden yürütülürse filtre/assembly çözümlemesi riski (çıkarım); normal build sahnesi Assets/Scenes/Game.unity (EditorBuildSettings.asset:8).
- Bilinçli istisna: Assets/Scripts/Heroes/HeroBase.cs:11 “Defender Chronicles 2 style” yorumu; yürütme etkisi yok.
- TestResults XML/log/JSON’ları tarihsel namespace/yol kayıtları; CCM obj dosyaları eski mutlak /Users/emreaydin/DefenderChronicles yolları taşır. Bunlar generated cache; sonraki restore/build davranışı Doğrulanamadı. Aktif kaynak/manifest/tools içinde yürütülebilir eski marka referansı bulunmadı (aynı grep).
- UserSettings layout ve My project/UserSettings layout kalıntıları editor görünüm/yol cache’i; paket/oyun ID’sini değiştirmez (çıkarım).

### Eşleşen dosyaların tam listesi
- `Assets/`: `InitTestScene759a220b-38e9-48c8-8a9b-14778e39ada0.unity`
- `Assets/Scripts/Heroes/`: `HeroBase.cs`
- `CampaignProgressionCCM/CampaignProgression.Tests/obj/`: `CampaignProgression.Tests.csproj.nuget.dgspec.json`; `project.assets.json`; `project.nuget.cache`
- `CampaignProgressionCCM/CampaignProgression.Tests/obj/Debug/net9.0/`: `CampaignProgression.Tests.GeneratedMSBuildEditorConfig.editorconfig`; `CampaignProgression.Tests.csproj.FileListAbsolute.txt`
- `CampaignProgressionCCM/CampaignProgression.Tests/obj/Release/net9.0/`: `CampaignProgression.Tests.GeneratedMSBuildEditorConfig.editorconfig`; `CampaignProgression.Tests.csproj.FileListAbsolute.txt`
- `CampaignProgressionCCM/CampaignProgression/obj/`: `CampaignProgression.csproj.nuget.dgspec.json`; `project.assets.json`; `project.nuget.cache`
- `CampaignProgressionCCM/CampaignProgression/obj/Debug/net9.0/`: `CampaignProgression.GeneratedMSBuildEditorConfig.editorconfig`; `CampaignProgression.csproj.FileListAbsolute.txt`
- `CampaignProgressionCCM/CampaignProgression/obj/Release/net9.0/`: `CampaignProgression.GeneratedMSBuildEditorConfig.editorconfig`; `CampaignProgression.csproj.FileListAbsolute.txt`
- `My project/UserSettings/Layouts/`: `default-6000.dwlt`
- `ProjectSettings/`: `ProjectSettings.asset`
- `TestResults/`: `acceptance-actor-metrics.json`; `acceptance-actor-rig.json`; `acceptance-building-metrics.json`; `ambient-editmode-final.xml`; `ambient-editmode.xml`
- `TestResults/`: `ambient-playmode-final.xml`; `ambient-playmode-retry.xml`; `ambient-playmode.xml`; `audio-editmode-combat.xml`; `audio-editmode-final.xml`
- `TestResults/`: `audio-editmode-serialized.xml`; `audio-editmode.xml`; `audio-playmode-combat.xml`; `audio-playmode-final.xml`; `audio-playmode-serialized.xml`
- `TestResults/`: `audit-remediation-editmode.xml`; `audit-remediation-playmode.xml`; `battle-premium-editmode.xml`; `battle-premium-playmode.xml`; `battle-premium-world-playmode.xml`
- `TestResults/`: `brand-editmode-final.log`; `brand-editmode-final.xml`; `brand-editmode.log`; `brand-editmode.xml`; `brand-evidence-suite.log`
- `TestResults/`: `brand-playmode-final.log`; `brand-playmode-final.xml`; `brand-playmode.log`; `brand-playmode.xml`; `brand-ratio-capture.log`
- `TestResults/`: `brand-ratio-capture.xml`; `brand-ratio-player-capture.log`; `brand-ratio-player-capture.xml`; `briefing-editmode.xml`; `briefing-playmode.xml`
- `TestResults/`: `closeout-actor-metrics-final.json`; `closeout-actor-metrics.json`; `closeout-actor-rig-final.json`; `closeout-actor-rig-sources.json`; `closeout-art-builder-2.log`
- `TestResults/`: `closeout-art-builder-final.log`; `closeout-art-builder-rigged.log`; `closeout-art-builder-siege.log`; `closeout-art-builder.log`; `closeout-building-metrics-final.json`
- `TestResults/`: `closeout-building-metrics.json`; `closeout-editmode-art-2.xml`; `closeout-editmode-art.xml`; `closeout-editmode-final-art-2.xml`; `closeout-editmode-final-art.xml`
- `TestResults/`: `closeout-editmode-green.xml`; `closeout-editmode-precommit.xml`; `closeout-hud-width-fix.xml`; `closeout-playmode-art.xml`; `closeout-playmode-final-art.xml`
- `TestResults/`: `closeout-playmode-green.xml`; `closeout-playmode-orientation-fix.xml`; `closeout-playmode-returning.xml`; `final-editmode.xml`; `final-playmode.xml`
- `TestResults/`: `first-impression-playmode.xml`; `frontend-editmode-final.xml`; `frontend-editmode.xml`; `frontend-playmode-final.xml`; `frontend-playmode.xml`
- `TestResults/`: `hud-landscape-editmode-filtered.xml`; `hud-landscape-editmode.xml`; `hud-landscape-playmode.xml`; `hud-phase-layout-playmode.xml`; `main-menu-editmode.xml`
- `TestResults/`: `main-menu-playmode.xml`; `mobile-editmode-final.xml`; `mobile-runtime-editmode.xml`; `pause-main-menu-hud-isolated.xml`; `pause-main-menu-playmode-final.xml`
- `TestResults/`: `pause-main-menu-playmode-rerun.xml`; `pause-main-menu-playmode.xml`; `portrait-editmode-final.xml`; `portrait-editmode-initial.xml`; `portrait-playmode-final.xml`
- `TestResults/`: `portrait-vertical-map-editmode.xml`; `portrait-vertical-map-playmode.xml`; `precommit-editmode.xml`; `precommit-playmode.xml`
- `UserSettings/Layouts/`: `CurrentMaximizeLayout.dwlt`; `default-6000.dwlt`

## 6. UI dışı sonraki büyük işler — en çok 8 öneri
Efor tahmini kişi-gün; ürün kapsamı ve cihaz sorunlarına göre değişir, aşağıdaki maddeler çıkarımdır.
1. **Yayın kimliği/yasal URL/hesap silme kararı ve uygulaması — 3–7 gün.** Hesap gerçekten bağlanabiliyor; boş link/silme eksikliği oyuncu verisini yönetmeyi tamamlamıyor (Screens.cs:294; tema.asset:41; M3).
2. **Rename sonrası temiz derleme + tüm EditMode/PlayMode + iki cihaz smoke — 1–3 gün.** Eylül XML’leri bugünkü 470 dosyalık değişikliği kapsamaz; geçici test sahnesi eski assembly taşıyor (XML:1; InitTestScene:152). Unity kilidi kalkınca yapılmalı.
3. **İlk 10 bölümün denge/gerçek süre doğrulaması — 3–6 gün.** Dalga başına çok sayıda düşman ve seviye açılışları var; hedef süre JSON’da, gerçek tamamlanma/yenilgi dağılımı bu audit’te Doğrulanamadı (FirstRegionCampaign.json; BattleTelemetry.cs:46).
4. **Valor/heroXP ürün döngüsünü bitir ya da birikimi kaldır — 4–10 gün.** Para birimi kazanılıyor, kullanım/stat etkisi yok; ekonomik karar UI’dan önce netleşmeli (CampaignProgressStore.cs:433; M2).
5. **Sunucu kayıt/eşitleme ve hesap yaşam döngüsü cihaz kabulü — 2–5 gün.** Misafir import, claim batch, v1→v2 uyumluluğu var; gerçek deploy/kimlik değişimi/çevrimdışı tekrar gönderim Doğrulanamadı (UnityGameServicesController.cs:171; CampaignProgressionModule.cs:52,201).
6. **GameSessionController ve eski oyun yolunu ayırma — 4–8 gün.** 3513 satırlık oturumda gameplay/capture/UI/otomasyon birlikte; 14 bağlantısız aday bakım yükü yaratıyor (satır sayımı; GameSessionController.cs:1350,2042; prototip taraması).
7. **11–50 içerik için kapsam kararı ve özgün oynanış doğrulaması — 8–20 gün.** Üretilen çok girişli haritalar ve katalog var; normal akış gizli, ayrı boss davranışları/elle tasarlanmış 50 harita kanıtı yok (CampaignLevelFactory.cs:175; ClosedTest.cs:12).
8. **Dağıtım imzası, crash/ölçüm ve fiziksel cihaz performans kabulü — 3–7 gün.** İmza ayarları boş ve yerel telemetri dışında servis entegrasyonu yok; araçların varlığı sonuç kanıtı değildir (ProjectSettings.asset:249,276; BattleTelemetry.cs:46; M3; tools/).
