# Defender Chronicles — Gerçek Durum Listesi

Son güncelleme: 2026-10-11

Bu belge eski “belgelendi = tamamlandı” yaklaşımını kaldırır. Aşağıdaki durumlar kabul kapısının gerçek seviyesini gösterir:

- **Doğrulandı:** Kodlandı ve ilgili otomatik/Simulator kabulü geçti.
- **Uygulandı:** Kod mevcut; bütün hedef cihaz, denge veya kullanıcı kabulleri henüz geçmedi.
- **Tasarlandı:** Yalnız belge, şema veya karar mevcut; tamamlanmış sayılmaz.
- **Bekliyor:** Henüz bu teslimin parçası değil.

## Ürün ve teknik temel

| Maddeler | Durum | Kanıt / kalan iş |
|---|---|---|
| DC-001–010 | Tasarlandı | Ürün ve kapsam belgeleri var; dış oyuncu doğrulaması yok. |
| DC-011 | Doğrulandı | Unity 6000.4.1f1 ve paket kilidiyle build alındı. |
| DC-012 | Tasarlandı | Kod envanteri belgeli; eski prototip kodu hâlâ arşiv olarak projede. |
| DC-013 | Uygulandı | `CombatMath`, tanımlar ve oturum ayrıldı; daha fazla saf mantık ayrıştırması mümkün. |
| DC-014 | Doğrulandı | Boot, Preparing, Wave, Intermission, Paused, Victory, Defeat çalışıyor. |
| DC-015–016 | Uygulandı | Tanım/ID alanları ve ilk bölge JSON verisi var; canlı kule/düşman denge değerleri hâlâ C# literal’larında, ScriptableObject denge kataloğu açık. Kaynak: `Assets/Scripts/Data/FirstRegionCampaign.cs`, `Assets/Scripts/Data/TowerProgression.cs`, `Assets/Scripts/Data/GreyboxDefinitions.cs`. |
| DC-017–018 | Doğrulandı | Durum/altın/can olayları ve pause zaman yönetimi çalışıyor. |
| DC-019–020 | Uygulandı | Kozmetik VFX havuzu var; düşman/ölüm echo havuzu ve tohumlu rastgelelik açık. Kaynak: `Assets/Scripts/Presentation/DefenderVfxPool.cs`; Katman 2 performans işleri. |
| DC-021 | Uygulandı | Bootstrap oturum/sunum doğrulaması yanında AppFlow ve servis bağlantısını kurar; yalnız oturum oluşturmaya sınırlı değildir. Kaynak: `Assets/Scripts/Core/GameBootstrap.cs`. |
| DC-022–024 | Uygulandı | Addressables ve yerel profil/kayıt/checkpoint akışı mevcut; tam geliştirici menüsü ve migration kapsamı açık. Kaynak: `Assets/Scripts/Core/CampaignProgressStore.cs`, `Assets/Scripts/Core/GameSessionController.Checkpoints.cs`. |
| DC-025 | Doğrulandı | Development runtime error/exception kaydı ve sıfır hata kapısı var. |

## Harita, savaş ve Knight

| Maddeler | Durum | Kanıt / kalan iş |
|---|---|---|
| DC-026–030 | Doğrulandı | XZ rota, rota ilerlemesi, 8 sabit slot ve mobil kurulum akışı çalışıyor. |
| DC-031 | Doğrulandı | Sv1→Sv2 ve Sv3 uzmanlaşması var: Archer RapidFire/Sharpshooter, Barracks ShieldGuard/AssaultGuard, Mage Blast/Frost. Kaynak: `Assets/Scripts/Data/TowerProgression.cs`, `Assets/Scripts/Core/GreyboxCombatants.cs`. |
| DC-032–037 | Doğrulandı | Toplam yatırımın aşağı yuvarlanmış %60 iadesi, geçici bozma, görünür yol, sabit muhafız ve slot/karakol dokunma akışı var. Kaynak: `Assets/Scripts/Core/CombatMath.cs`, `Assets/Scripts/Core/GameSessionController.cs`. |
| DC-038–039 | Doğrulandı | Üç engelleyici/yeniden doğan kışla askeri; bozulan kışlada saldırı/hareket durur, konuşlanmış askerler bloklar. Kaynak: `Assets/Scripts/Data/TowerProgression.cs`, `Assets/Scripts/Core/GreyboxCombatants.cs`. |
| DC-040–041 | Tasarlandı | Ayrı menzilli/büyü dost birlikleri revize dikey dilimde yok. |
| DC-042–044 | Uygulandı | En ileride hedefleme, Sharpshooter destek/kuşatma önceliği, Vanguard %50 fiziksel direnci, slow/stun var; diğer prototip stratejileri ve tam direnç matrisi açık. Kaynak: `Assets/Scripts/Core/GreyboxCombatants.cs`, `Assets/Scripts/Core/GameSessionController.Battle.cs`. |
| DC-045–046 | Uygulandı | Hasardan bağımsız kozmetik VFX havuzu var; gelişmiş kümelenme kapsam dışı. |
| DC-047 | Doğrulandı | Düşman temizliği, asker yeniden doğması ve Knight 5 sn dönüşü var; terminal fazda kalan aktörler aynı adımda durur. Kaynak: `Assets/Scripts/Core/GreyboxCombatants.cs`, `Assets/Scripts/Core/GameSessionController.Battle.cs`. |
| DC-048 | Doğrulandı | Takım rengi, siluet, animasyon ve sağlık çubuğu production sunumunda okunur. |
| DC-049–050 | Uygulandı | Havuzlu VFX/ses; oyuncu hızları 1×/2×/4×/8× ve pause sonrası hız korunur. Kaynak: `Assets/Scripts/Core/GameSessionController.cs`; cihaz performans kabulü açık. |
| DC-051–055 | Doğrulandı | Knight sabit muhafız: 20 hasar/0.35 sn, boşta cooldown borcu yok, 5 sn dönüş, Shield Bash ve HUD. Kaynak: `Assets/Scripts/Core/GreyboxCombatants.cs`, `Assets/Scripts/Core/CombatMath.cs`. |
| DC-056–060 | Uygulandı | İlk bölüm zaferinde heroXP birikir ve kahraman adı ayarı var; savaş içi seviye/stat seçimi, ekipman ve loadout açık. Kaynak: `Assets/Scripts/Core/CampaignProgressStore.cs`, `Assets/Scripts/UI/AppFlowController.Hero.cs`. |

## Düşman, dalga ve ekonomi

| Maddeler | Durum | Kanıt / kalan iş |
|---|---|---|
| DC-061 | Doğrulandı | Raider, Vanguard, Regenerator, Support, Siege ve boss ayrı production siluetlerine sahip. |
| DC-062–068 | Doğrulandı | Altı arketip; boss %66/%33 fazlarını yalnız ileri/sıralı işler, %66’da tek altı Raider takviyesi, %33’te hız ×1.44. Bozma ayrı zamanlayıcıdır. Kaynak: `Assets/Scripts/Core/GreyboxCombatants.cs`, `Assets/Scripts/Core/GameSessionController.Battle.cs`. |
| DC-069 | Uygulandı | İlk 10 bölüm JSON’dan `WaveDefinition` olarak yüklenir; dalga asset editörü bekliyor. Bölüm 10: 400 altın, 7+1 dalga, son boss startDelay 36 sn. Kaynak: `Assets/Resources/FirstRegionCampaign.json`, `Assets/Scripts/Data/FirstRegionCampaign.cs`, `Assets/Scripts/Data/CampaignCatalog.cs`. |
| DC-070 | Tasarlandı | Otomatik tehdit bütçesi uygulanmadı. |
| DC-071 | Doğrulandı | Sonraki ana tehdit HUD’da gösteriliyor. |
| DC-072 | Uygulandı | Erken dalga düğmesi var; kalan süreye göre ek ödül yok. |
| DC-073 | Doğrulandı | Tek savaş kaynağı altın. |
| DC-074–075 | Doğrulandı | Üç ağırlıklı stratejinin otomatik kabulü Katman 1’de geçti; manuel/dış oyuncu kabulü açık. Archer Sv1 24, Sv2 ×1.75/×1.44; genel diğer kuleler ×1.55/×1.2, Barracks asker Sv2 32 hasar/210 HP istisnası. Kaynak: `Assets/Scripts/Data/TowerProgression.cs`, `Assets/Tests/PlayMode/AuditLayer1BalanceTests.cs`. |
| DC-076–082 | Uygulandı | Can oranına göre yıldız, Valor/heroXP bölüm ödülü ve kayıt var; Valor harcama, rastgele güç, ekipman, zorluk seçimi ve yan görev açık. Kaynak: `Assets/Scripts/Combat/BattleRules.cs`, `Assets/Scripts/Core/CampaignProgressStore.cs`. |

## Mobil HUD ve sanat

| Maddeler | Durum | Kanıt / kalan iş |
|---|---|---|
| DC-083–084 | Doğrulandı | Prefab tabanlı TMP HUD, ortak panel dili, ikon/şekil desteği ve safe-area yerleşimi var. |
| DC-085–087 | Doğrulandı | Can, altın, dalga, Knight, karakol rol/değer/yükseltme/satış bilgileri çalışıyor. |
| DC-088 | Uygulandı | Shield Bash önizleme ve cooldown var; ayrı hedefleme/iptal modu yok. |
| DC-089 | Doğrulandı | İlk üç dalga için kısa oyun içi öğretici mesajları var. |
| DC-090–094 | Uygulandı | TR/EN arayüz, menü/kampanya/brifing/ayarlar mevcut; 11–50 tehdit metinlerinin EN çevirisi uygulandı; UX kabul takibi kod worker’ında. Ses tercihleri savaş müziği/SFX/ambiyansa anında uygulanır. Kaynak: `Assets/Scripts/UI/AppFlowController.cs`, `Assets/Scripts/UI/FrontendPreferences.cs`, `Assets/Scripts/Presentation/DefenderAudioController.cs`, `Assets/Scripts/Presentation/DefenderAmbientAudioController.cs`. |
| DC-095 | Doğrulandı | HUD ayrı safe-area panelinde ve yatay Dynamic Island Simulator’da görünür. |
| DC-096 | Uygulandı | Özgün 1024×1024 uygulama ikonu üretildi. |
| DC-097–105 | Doğrulandı | Özgün Blender/FBX seti, stilize görsel durumlar, VFX havuzu, 22 ses olayı ve 6 müzik durumu production kataloğunda. |

## Kalite ve sürüm kapıları

| Maddeler | Durum | Kanıt / kalan iş |
|---|---|---|
| DC-106–109 | Tasarlandı | Erişilebilirlik ve performans bütçeleri gerçek cihaz geçişi bekliyor. |
| DC-110 | Doğrulandı | Katman 1 final EditMode 141/141; kanıt `/tmp/dc-audit/fix-runs/audit-layer1/final-edit.xml`, kaynak testler `Assets/Tests/EditMode/`. Bu belge görevi testleri tekrar çalıştırmadı. |
| DC-111 | Doğrulandı | Katman 1 final PlayMode 89/89; kanıt `/tmp/dc-audit/fix-runs/audit-layer1/final-play.xml`, kaynak testler `Assets/Tests/PlayMode/`. Cihaz/dış oyuncu kapıları ayrı. |
| DC-112 | Uygulandı | Atomik yerel profil/kampanya kaydı, kurtarma ve dalga checkpoint’i var. Kaynak: `Assets/Scripts/Core/CampaignProgressStore.cs`, `Assets/Scripts/Core/GameSessionController.Checkpoints.cs`; ek sınır testleri Katman 4’te. |
| DC-113 | Uygulandı | Açılış ve zafer görüntüleri saklandı; otomatik piksel karşılaştırması yok. |
| DC-114 | Bekliyor | 30–60 dakikalık dayanıklılık testi yapılmadı. |
| DC-115 | Uygulandı | Input olayları null-safe; fiziksel iPhone build/telemetri akışı hazır, gerçek hızlı/çoklu dokunma matrisi bekliyor. |
| DC-116 | Uygulandı | Hızlandırılmış tam bölüm kabul modu var; istatistiksel denge simülatörü yok. |
| DC-117 | Bekliyor | 5–10 dış oyuncu testi yapılmadı. |
| DC-118 | Doğrulandı | Final Development Simulator açılışı ve tam koşularda yakalanmış error/exception yok. |
| DC-119 | Doğrulandı | 16 Ağustos 2026 tarihli eski iOS kabulünde iki tam boss zaferi; güncel Bölüm 10 kodu 7 normal + 1 boss dalgasıdır (`Assets/Resources/FirstRegionCampaign.json`). Güncel fiziksel cihaz kabulü açık. |
| DC-120 | Doğrulandı | Archer/Barracks/Mage ağırlıklı otomatik kabul Katman 1’de yeşil; manuel üç strateji kabulü açık. Kaynak: `Assets/Tests/PlayMode/StrategyBalanceAcceptanceTests.cs`, `Assets/Tests/PlayMode/AuditLayer1BalanceTests.cs`. |
| DC-121 | Doğrulandı | Tek haritanın tüm aktörleri, karakolları, HUD, VFX, ses ve müziği sanatsal dikey dilime taşındı. |
| DC-122 | Bekliyor | iPhone 13 Development ölçüm akışı hazır; normal hızlı fiziksel cihaz kabul koşusu henüz yapılmadı. |
| DC-123 | Bekliyor | Dış kullanıcı testi yapılacak. |
| DC-124–128 | Uygulandı | Kampanya/hesap/çevrimdışı claim akışı var; normal ön yüz ilk 10 bölüme sınırlı, katalog 5 bölge/50 bölüm. Liderlik, gelir ve mağaza teslimi açık. Kaynak: `Assets/Scripts/Data/CampaignCatalog.cs`, `Assets/Scripts/UI/AppFlowController.ClosedTest.cs`, `Assets/Scripts/Core/CampaignProgressStore.cs`. |

## Sanatsal dikey dilim kabul özeti

- **Geçti:** production katalog doğrulaması, özgün görsel set, TMP HUD, Türkçe kopya, havuzlu VFX, mixer/müzik durumları, sekiz sabit slot, üç karakol, sabit Knight, Bölüm 10’da 7+1 dalga, boss, zafer, yenilgi, pause ve restart.
- **Kısmi:** gerçek parmakla hızlı/çoklu dokunma, üç ayrı stratejinin manuel/dış oyuncu dengesi ve bölüm bazlı dış oyuncu süre ölçümü.
- **Sonraki kapı:** fiziksel iPhone'da dokunma, bellek, termal durum ve kesin 30/60 FPS performans testi.

## Katman 1 — Tamamlanan audit düzeltmeleri

| Maddeler | Durum | Kanıt / kalan iş |
|---|---|---|
| K1-01 | Doğrulandı | Knight boşta saldırı biriktirmez; cooldown clamp/reset ve 0.35 sn periyot. `Assets/Scripts/Core/CombatMath.cs`, `Assets/Scripts/Core/GreyboxCombatants.cs`; `AuditCombatRulesPlayTests`. |
| K1-02 | Doğrulandı | Boss sızması normal hasar + tek yıldız can tavanı (20→7); otomatik yenilgi değil, can tükenirse Defeat. Tüm dalgalar biter ve can kalırsa en fazla bir yıldızla Victory. `Assets/Scripts/Combat/BattleRules.cs`, `Assets/Scripts/Core/GameSessionController.cs`; `AuditBossVictoryTests`. |
| K1-03 | Doğrulandı | Boss safhaları monoton/sıralı; %66 takviyesi tek sefer, eşik geçişi bozma saldırısı tetiklemez. `Assets/Scripts/Core/GreyboxCombatants.cs`, `Assets/Scripts/Core/GameSessionController.Battle.cs`; `AuditCombatRulesPlayTests`. |
| K1-04 | Doğrulandı | Müzik/SFX tercihi savaşta ve ambiyansta anlık; kayıtsız varsayılan 1, nominal seviyeler korunur. `Assets/Scripts/UI/FrontendPreferences.cs`, `Assets/Scripts/Presentation/DefenderAudioController.cs`, `Assets/Scripts/Presentation/DefenderAmbientAudioController.cs`; `AuditAudioSettingsTests`. |
| K1-05 | Doğrulandı | Bozuk kışlanın askerleri hareket/saldırıyı durdurur, konuşlanmış olanlar bloklar; Victory/Defeat sonrası kalan aktör tick’leri durur. `Assets/Scripts/Core/GreyboxCombatants.cs`, `Assets/Scripts/Core/GameSessionController.Battle.cs`; `AuditCombatRulesPlayTests`. |
| K1-06 | Doğrulandı | Yükseltme/uzmanlaşma sonrası seçili menzil halkası yenilenir. `Assets/Scripts/Core/GameSessionController.cs`, `Assets/Scripts/Core/GameSessionController.Battle.cs`; `AuditBossVictoryTests`. |
| K1-07 | Doğrulandı | Archer/Barracks denge telafisi ve yalnız Bölüm 10 final boss başlangıcı 36 sn; mevcut ve ek strateji kabulleri yeşil. `Assets/Scripts/Data/TowerProgression.cs`, `Assets/Resources/FirstRegionCampaign.json`; `AuditLayer1BalanceTests`. |

Katman 1 sonuç kanıtı: `/tmp/dc-audit/fix-reports/audit-layer1.md` ve `fix-runs/audit-layer1/final-edit.xml` / `final-play.xml`. Aşağıdaki Katman 2/3/4 maddeleri 11 Ekim audit’inden aktarılmıştır; HEAD’e gelen Katman 2 UX commit’leri Uygulandı olarak işlendi, cihaz/tam suite kabul takibi ayrı kalır. Önerilen denge sayıları mevcut kural değildir.

## Katman 2 — UX, performans ve cihaz ölçümü

| Maddeler | Durum | Kanıt / kalan iş |
|---|---|---|
| K2-01 | Uygulandı | SAT ilk dokunuşta 3 sn gerçek zamanlı onay, ikinci dokunuşta satış; süre/selection/panel/faz değişimi iptal eder. `Assets/Scripts/Presentation/DefenderHudController.Sales.cs`, `Assets/Scripts/Core/GameSessionController.Sales.cs`; UX test/kabul takibi kod worker’ında. |
| K2-02 | Uygulandı | HUD/frontend butonlarının görünmez hedefi en az 112×112 canvas birimi; belirtilen 0.39842 pt/birim ölçeğinde ≥44 pt. `Assets/Scripts/UI/MobileTouchTarget.cs`; fiziksel cihaz kabulü açık. |
| K2-03 | Uygulandı | Uzmanlaşma modalı savaşı mevcut pause mekanizmasıyla durdurur; kapanınca modalın aldığı pause/hız geri yüklenir, önceden pause olan oturum pause kalır. `Assets/Scripts/Core/GameSessionController.Modals.cs`, `Assets/Scripts/Presentation/DefenderHudController.Specializations.cs`; UX test/kabul takibi kod worker’ında. |
| K2-04 | Doğrulandı | Yükseltme sonrası menzil halkası Katman 1’de tamamlandı (K1-06); yeni açık iş değil. |
| K2-05 | Uygulandı | Bölüm 11–50 tehdit metinleri TR/EN üretilir; dil değişince aktif önizleme yenilenir. `Assets/Scripts/Data/CampaignLevelFactory.cs`, `Assets/Scripts/Core/GameSessionController.cs`; normal ön yüz hâlâ 10 bölümle sınırlı. |
| K2-06 | Bekliyor | `Assets/Scripts/Presentation/DefenderAudioController.cs` LINQ kullanımını tahsissiz döngüye çevir. |
| K2-07 | Bekliyor | AoE `ToArray()` yerine yeniden kullanılan buffer; `Assets/Scripts/Core/GameSessionController.cs`. |
| K2-08 | Bekliyor | `renderer.material` tahsislerini sharedMaterial/property block ile azalt; `Assets/Scripts/Core/GreyboxCombatants.cs` ve sunum kaynakları. |
| K2-09 | Bekliyor | Incremental GC’yi aç ve cihazda ölç; `ProjectSettings/ProjectSettings.asset`. |
| K2-10 | Bekliyor | Düşman ve ölüm echo havuzu; `Assets/Scripts/Core/GameSessionController.cs` / sunum katmanı. |
| K2-11 | Bekliyor | Fiziksel iPhone’da 1× bölüm ve 30 dk koşu; `docs/IOS_DEVICE_ACCEPTANCE_2026-08-16.md` kabul kanıtı. |
| K2-12 | Bekliyor | Release cihaz build yolu ekle; `tools/build_and_run_ios_device.sh`. |
| K2-13 | Bekliyor | PASS kapısına bellek ve FPS düşüşü sınırlarını kat; cihaz kabul ölçüm akışı. |

## Katman 3 — Oynanış ve tasarım denemeleri

| Maddeler | Durum | Kanıt / kalan iş |
|---|---|---|
| K3-01 | Tasarlandı | İlk üç bölümün aynı Raider dalgalarını çeşitlendir: B2 Vanguard, B3 kümeler, ilk kule hedefi ≤15 sn. `Assets/Resources/FirstRegionCampaign.json`; öneri, mevcut denge değil. |
| K3-02 | Tasarlandı | Dal ekonomi denemesi: RapidFire 160→100, Sharpshooter 160→120 ve panelde DPS önizlemesi; mevcut iki Archer dalı 160 altın. `Assets/Scripts/Data/TowerProgression.cs`. |
| K3-03 | Tasarlandı | Erken çağrı için kalan saniye başına 1 altın, en fazla 8 dene; mevcut ödül sıfır. `Assets/Scripts/Core/GameSessionController.cs`, `StartNextWave`. |
| K3-04 | Tasarlandı | Biriken Valor/heroXP için iki Bash varyantı seçimi ve üç isteğe bağlı rozet; mevcut harcama yok. `Assets/Scripts/Core/CampaignProgressStore.cs`. |
| K3-05 | Tasarlandı | %40 büyü dirençli hafif düşman ve B6’da ikinci girişli harita dene; mevcut büyü direnci yok. `Assets/Scripts/Core/GreyboxCombatants.cs`, `Assets/Scripts/Data/CampaignLevelFactory.cs`. |

## Katman 4 — Mimari ve kalan test kapsamı

| Maddeler | Durum | Kanıt / kalan iş |
|---|---|---|
| K4-01 | Bekliyor | Ölü/prefab’a bağlı prototip kodunu prefab’larıyla envanterle ve arşivle; canlı/ölü ayrımını yeniden doğrula. `Assets/Scripts/` eski Heroes/Units/Towers/Projectiles/Enemies ve yönetici sınıfları. |
| K4-02 | Tasarlandı | `Assets/Scripts/Core/GameSessionController.cs` → Economy / SlotController / WaveDirector / BattleClock / InputRouter / HudBinding / AcceptanceRunner ayrıştırması. |
| K4-03 | Tasarlandı | Literal denge değerlerini sürümlü BalanceCatalog’a taşı; tanımlı ScriptableObject’lerin canlı tüketimini kur. `Assets/Scripts/Data/TowerProgression.cs`, `Assets/Scripts/Data/GreyboxDefinitions.cs`. |
| K4-04 | Bekliyor | UnityEngine.Object üzerindeki `?.` / `??=` kullanımını açık null kontrollerine çevir; `Assets/Scripts/Core/GreyboxCombatants.cs`, `Assets/Scripts/Core/GameSessionController.cs` ve diğer canlı kaynaklar. |
| K4-05 | Bekliyor | `tools/generate_unity_assets.py` script GUID’lerini korusun veya araç arşivlensin; prefab bağlarını koparma riski. |
| K4-06 | Bekliyor | Audit’in test boşluğu listesini Katman 1 kapsamına göre yeniden değerlendir: boss eşik sınırları/bekleyen takviye, atomik altın/iade sınırları, aktif dalga pause/background, gerçek sahne restart, Knight tam dönüş eşiği, son asker ölümü/satışı, tekrarlı terminal/removal idempotency ve bozuk checkpoint matrisi. Faz sırası/tek takviye, bozulan asker ve terminal tick stop testleri K1’de eklendi; bunları yeniden eksik sayma. Kaynak: `Assets/Tests/`, audit `reports/06-testler.md`. |
