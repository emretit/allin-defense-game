namespace AllinDefense.UI
{
    /// <summary>A bilingual label bound explicitly, independent of its Turkish wording.</summary>
    public readonly struct LocalizedCopy
    {
        public string Turkish { get; }
        public string English { get; }
        public bool IsLocalized { get; }
        public string Value => GameLocalization.Text(Turkish, English);
        public string Format(params object[] parameters) => string.Format(Value, parameters);

        public LocalizedCopy(string turkish, string english)
        {
            Turkish = turkish ?? string.Empty;
            English = english ?? string.Empty;
            IsLocalized = true;
        }

        private LocalizedCopy(string literal)
        {
            Turkish = English = literal ?? string.Empty;
            IsLocalized = false;
        }

        // Empty, numeric and dynamically refreshed strings remain literal copy.
        public static implicit operator LocalizedCopy(string literal) => new(literal);
    }

    /// <summary>
    /// Stable frontend copy IDs. Change either language here without changing call sites
    /// or test-visible GameObject names; widgets never infer an ID from displayed text.
    /// </summary>
    public static class FrontendCopy
    {
        public static readonly LocalizedCopy PauseRestart = new("YENİDEN BAŞLAT", "RESTART");
        public static readonly LocalizedCopy BattlePhase = new("SAVAŞTA", "IN BATTLE");
        public static readonly LocalizedCopy BossPhase = new("BOSS", "BOSS");
        public static readonly LocalizedCopy UpcomingCount = new("×{0}", "×{0}");
        public static readonly LocalizedCopy BossName = new("Bölge Lordu", "Region Lord");
        public static readonly LocalizedCopy LeaveBattleTitle = new("HARİTAYA DÖNÜLSÜN MÜ?", "RETURN TO THE MAP?");
        public static readonly LocalizedCopy LeaveBattleHint = new("Bu savaştan ayrılacaksın. Haritaya dönmek istiyor musun?", "You will leave this battle. Return to the map?");
        public static readonly LocalizedCopy EarlyWaveReward = new("SONRAKİ DALGA  +{0}", "NEXT WAVE  +{0}");
        public static readonly LocalizedCopy Seconds = new("{0} sn", "{0} sec");
        public static readonly LocalizedCopy SwitchOn = new("AÇIK", "ON");
        public static readonly LocalizedCopy SwitchOff = new("KAPALI", "OFF");
        public static readonly LocalizedCopy KitPreview = new("ÖNİZLEME", "PREVIEW");
        public static readonly LocalizedCopy KitConfirm = new("TEKRAR DOKUN · KUR", "TAP AGAIN · BUILD");
        public static readonly LocalizedCopy CampaignEyebrow = new("GREENWATCH SEFERİ", "THE GREENWATCH CAMPAIGN");
        public static readonly LocalizedCopy BattleResult = new("SAVAŞ SONUCU", "BATTLE RESULT");
        public static readonly LocalizedCopy TryAgain = new("YENİDEN DENE", "TRY AGAIN");
        public static readonly LocalizedCopy NextLevel = new("SONRAKİ BÖLÜM  ›", "NEXT LEVEL  ›");
        public static readonly LocalizedCopy ReturnToMap = new("HARİTAYA DÖN", "RETURN TO MAP");
        public static readonly LocalizedCopy MyHero = new("KAHRAMANIM", "MY HERO");
        public static readonly LocalizedCopy HeroNameDescription = new("Kahramanına bir ad ver. Savaşta bu adla görünecek.", "Name your hero. This name will appear in battle.");
        public static readonly LocalizedCopy Warrior = new("Savaşçı", "Warrior");
        public static readonly LocalizedCopy HeroNameHint = new("En fazla 20 karakter. Boş bırakırsan Savaşçı olur.", "Up to 20 characters. Leave blank to use Warrior.");
        public static readonly LocalizedCopy Cancel = new("İPTAL", "CANCEL");
        public static readonly LocalizedCopy Save = new("KAYDET", "SAVE");
        public static readonly LocalizedCopy SettingsButton = new("AYAR", "SETTINGS");
        public static readonly LocalizedCopy MainMenuCallout = new("SAVUNMA HATTI\nSENİ BEKLİYOR", "THE DEFENCE LINE\nAWAITS");
        public static readonly LocalizedCopy RealmMap = new("KRALLIK HARİTASI", "REALM MAP");
        public static readonly LocalizedCopy StartDefence = new("SAVUNMAYI BAŞLAT", "START DEFENCE");
        public static readonly LocalizedCopy WelcomeEyebrow = new("GREENWATCH  •  SINIR SEFERİ", "GREENWATCH  •  FRONTIER CAMPAIGN");
        public static readonly LocalizedCopy WelcomeDescription = new("Greenwatch Kalesi kuşatma altında.\nSavunma hattını kur. Şafağa kadar dayan.", "Greenwatch Keep is under siege.\nBuild the defence line. Hold until dawn.");
        public static readonly LocalizedCopy LinkAccount = new("HESABINI BAĞLA", "LINK YOUR ACCOUNT");
        public static readonly LocalizedCopy GuestNote = new("Misafir ilerlemen bu cihazda saklanır; hesabını daha sonra bağlayabilirsin.", "Guest progress is saved on this device; you can link an account later.");
        public static readonly LocalizedCopy ProtectProgress = new("İLERLEMENİ KORU", "PROTECT YOUR PROGRESS");
        public static readonly LocalizedCopy AccountDescription = new("Misafir olarak hemen oynayabilir veya ilerlemeni cihazlar arasında taşımak için hesabını bağlayabilirsin.", "Play as a guest now, or link your account to carry progress between devices.");
        public static readonly LocalizedCopy StartAsGuest = new("MİSAFİR OLARAK BAŞLA", "START AS GUEST");
        public static readonly LocalizedCopy PrivacyPolicy = new("GİZLİLİK POLİTİKASI", "PRIVACY POLICY");
        public static readonly LocalizedCopy TermsOfService = new("KULLANIM KOŞULLARI", "TERMS OF SERVICE");
        public static readonly LocalizedCopy SettingsTitle = new("AYARLAR", "SETTINGS");
        public static readonly LocalizedCopy Music = new("MÜZİK", "MUSIC");
        public static readonly LocalizedCopy Effects = new("EFEKTLER", "SFX");
        public static readonly LocalizedCopy Haptics = new("TİTREŞİM", "HAPTICS");
        public static readonly LocalizedCopy CameraShake = new("KAMERA SARSINTISI", "CAMERA SHAKE");
        public static readonly LocalizedCopy ReducedMotion = new("AZALTILMIŞ HAREKET", "REDUCED MOTION");
        public static readonly LocalizedCopy AccountManagement = new("HESAP YÖNETİMİ", "ACCOUNT MANAGEMENT");
        public static readonly LocalizedCopy ObjectiveArcher = new("Okçu kur ve kaleyi koru.", "Build an archer and protect the keep.");
        public static readonly LocalizedCopy ObjectiveBarracks = new("Kışlayla düşmanı menzilde tut.", "Hold enemies in range with barracks.");
        public static readonly LocalizedCopy ObjectiveMage = new("Büyücüyle kalabalığı temizle.", "Clear crowds with a mage.");
        public static readonly LocalizedCopy ObjectiveArmour = new("Zırhlı öncüye karşı büyü kullan.", "Use magic against the armoured vanguard.");
        public static readonly LocalizedCopy ObjectiveTowerRoles = new("Üç kule rolünü birlikte kullan.", "Combine all three tower roles.");
        public static readonly LocalizedCopy ObjectiveRegeneration = new("Yenilenen hedefe ateşi yoğunlaştır.", "Focus fire on regenerating enemies.");
        public static readonly LocalizedCopy ObjectiveHealer = new("Önce şifacıyı durdur.", "Stop the healer first.");
        public static readonly LocalizedCopy ObjectiveSiege = new("Kuşatma saldırısını darbeyle kes.", "Interrupt siege attacks with a strike.");
        public static readonly LocalizedCopy ObjectiveMixedPressure = new("Karma düşman baskısına hazırlan.", "Prepare for mixed enemy pressure.");
        public static readonly LocalizedCopy ObjectiveLord = new("Lordun kırmızı uyarısını darbeyle kes.", "Interrupt the lord's red warning.");
        public static readonly LocalizedCopy SaveRetry = new("Kayıt tekrar denenecek", "Save will retry");
        public static readonly LocalizedCopy SaveRecovery = new("Kayıt kurtarılmayı bekliyor", "Save needs recovery");
        public static readonly LocalizedCopy TransferReview = new("Aktarım kontrolü gerekiyor", "Transfer needs review");
        public static readonly LocalizedCopy WaitingForConnection = new("Bağlantı bekleniyor", "Waiting for connection");
        public static readonly LocalizedCopy Sell = new("SAT\n+{0}", "SELL\n+{0}");
        public static readonly LocalizedCopy Confirm = new("ONAYLA +{0}", "CONFIRM +{0}");
        public static readonly LocalizedCopy TowerLevel = new("SV {0}", "LV {0}");
        public static readonly LocalizedCopy SellingReturnsGold = new("Satışta {0} altın geri alırsın.", "Selling returns {0} gold.");
        public static readonly LocalizedCopy Upgrade = new("YÜKSELT\n{0}", "UPGRADE\n{0}");
        public static readonly LocalizedCopy Max = new("TAMAM", "MAX");
        public static readonly LocalizedCopy NeedMoreGoldToUpgrade = new("Yükseltme için {0} altın eksik.", "Need {0} more gold to upgrade.");
        public static readonly LocalizedCopy SpecializeGold = new("UZMANLAŞ\n{0} ALTIN", "SPECIALIZE\n{0} GOLD");
        public static readonly LocalizedCopy AfterLevel = new("BÖLÜM {0} SONRASI", "AFTER LEVEL {0}");
        public static readonly LocalizedCopy Gold = new("{0} ALTIN", "{0} GOLD");
        public static readonly LocalizedCopy NeedMoreGold = new("\n{0} altın eksik", "\nNeed {0} more gold");
        public static readonly LocalizedCopy BackSellAndRebuildToChangeBranch = new("GERİ • Dal değiştirmek için satıp yeniden kur", "BACK • Sell and rebuild to change branch");
        public static readonly LocalizedCopy SpecializationRapidFireName = new("Hızlı Atış", "Rapid Fire");
        public static readonly LocalizedCopy SpecializationSharpshooterName = new("Keskin Nişancı", "Sharpshooter");
        public static readonly LocalizedCopy SpecializationShieldGuardName = new("Kalkan Birliği", "Shield Guard");
        public static readonly LocalizedCopy SpecializationAssaultGuardName = new("Hücum Birliği", "Assault Guard");
        public static readonly LocalizedCopy SpecializationBlastName = new("Patlama", "Blast");
        public static readonly LocalizedCopy SpecializationFrostName = new("Buz", "Frost");
        public static readonly LocalizedCopy SpecializationRapidFireDescription = new("Hafif hedeflere hızlı atış", "Rapid shots · Light targets");
        public static readonly LocalizedCopy SpecializationSharpshooterDescription = new("Uzun menzil · Önce destek", "Long range · Support priority");
        public static readonly LocalizedCopy SpecializationShieldGuardDescription = new("Üç dayanıklı asker · Yol tutar", "Three tough guards · Holds route");
        public static readonly LocalizedCopy SpecializationAssaultGuardDescription = new("Üç asker · Yüksek hasar", "Three guards · High damage");
        public static readonly LocalizedCopy SpecializationBlastDescription = new("Geniş alanda büyü hasarı", "Magic damage over a wider area");
        public static readonly LocalizedCopy SpecializationFrostDescription = new("Düşmanları 2 sn %50 yavaşlatır", "Slows enemies by 50% for 2 sec");
        public static readonly LocalizedCopy RadialMissing = new("{0} eksik", "{0} short");
        public static readonly LocalizedCopy RadialTapAgain = new("Onay için tekrar dokun", "Tap again to confirm");
        public static readonly LocalizedCopy RadialSell = new("SAT", "SELL");
        public static readonly LocalizedCopy RadialConfirm = new("ONAYLA", "CONFIRM");
        public static readonly LocalizedCopy RadialStats = new("Hasar {0} · Menzil {1:0.0} · {2:0.0}/sn", "Damage {0} · Range {1:0.0} · {2:0.0}/s");
        public static readonly LocalizedCopy RadialUpgradeStats = new("Hasar {0}→{1}\nMenzil {2:0.0}→{3:0.0} · {4:0.0}→{5:0.0}/sn", "Damage {0}→{1}\nRange {2:0.0}→{3:0.0} · {4:0.0}→{5:0.0}/s");
        public static readonly LocalizedCopy AttackDifferences = new("Hasar {0}→{1}\nHız {2:0.0}→{3:0.0}/sn", "Damage {0}→{1}\nRate {2:0.0}→{3:0.0}/s");
        public static readonly LocalizedCopy GuardDifferences = new("Asker canı {0}→{1}\nBirlik canı {2}→{3}", "Guard health {0}→{1}\nSquad health {2}→{3}");
        public static readonly LocalizedCopy BlastDifferences = new("Hasar {0}→{1}\nAlan {2:0.0}→{3:0.0}", "Damage {0}→{1}\nArea {2:0.0}→{3:0.0}");
        public static readonly LocalizedCopy FrostDifferences = new("Hasar {0}→{1}\n%{2} yavaşlatma · {3:0} sn", "Damage {0}→{1}\n{2}% slow · {3:0}s");
        public static readonly LocalizedCopy BattlePaused = new("Savaş duraklatıldı", "Battle paused");
        public static readonly LocalizedCopy ChangeBranchHint = new("Dal değiştirmek için satıp yeniden kur", "Sell and rebuild to change branch");
        public static readonly LocalizedCopy ArcherRole = new("Tek hedef · Uzun menzil", "Single target · Long range");
        public static readonly LocalizedCopy BarracksRole = new("Üç asker · Yolu tutar", "Three guards · Blocks the route");
        public static readonly LocalizedCopy MageRole = new("Büyü · Alan hasarı", "Magic · Area damage");
        public static readonly LocalizedCopy TutorialBuild = new("Yuvaya dokun, okçu seç", "Tap a site, choose archer");
        public static readonly LocalizedCopy TutorialSecond = new("Bir okçu daha kur", "Build one more archer");
        public static readonly LocalizedCopy TutorialWave = new("İlk dalgayı başlat", "Start the first wave");
        public static readonly LocalizedCopy TutorialAbility = new("Yetenekle düşmanları sersemlet", "Stun enemies with your ability");
        public static readonly LocalizedCopy TutorialThird = new("Üçüncü kıvrıma okçu kur", "Build at the third bend");
        public static readonly LocalizedCopy TutorialUpgrade = new("Bir okçu seç, yükselt", "Select an archer, upgrade it");
        public static readonly LocalizedCopy TutorialDone = new("Hazırsın!", "You are ready!");
        public static readonly LocalizedCopy TutorialSkip = new("Biliyorum", "I know");
        public static readonly LocalizedCopy Returns = new("{0} dönüyor", "{0} returns");
        public static readonly LocalizedCopy BossApproaching = new("YAKLAŞAN BOSS", "BOSS APPROACHING");
        public static readonly LocalizedCopy BossPressure = new("BOSS BASKISI", "BOSS PRESSURE");
        public static readonly LocalizedCopy NextWave = new("SIRADAKİ TEHDİT", "NEXT WAVE");
        public static readonly LocalizedCopy CurrentWave = new("AKTİF BASKI", "CURRENT WAVE");
        public static readonly LocalizedCopy Result = new("SONUÇ", "RESULT");
        public static readonly LocalizedCopy Critical = new("KRİTİK", "CRITICAL");
        public static readonly LocalizedCopy HudVictoryHint = new("Greenwatch savunması geceyi atlattı. Bir sonraki kuşatmaya hazırlan.", "Greenwatch held through the night. Prepare for the next siege.");
        public static readonly LocalizedCopy HudDefeatHint = new("Kale düştü. Savunma düzenini güçlendir ve kuşatmayı yeniden başlat.", "The keep fell. Adjust your defence and try again.");
        public static readonly LocalizedCopy Level = new("Bölüm {0}", "Level {0}");
        public static readonly LocalizedCopy Build = new("KUR\n{0}", "BUILD\n{0}");
        public static readonly LocalizedCopy Status = new("DURUM", "STATUS");
        public static readonly LocalizedCopy Preparing = new("DALGA ÖNCESİ", "PREPARING");
        public static readonly LocalizedCopy Defending = new("SAVUNMA HATTI", "DEFENDING");
        public static readonly LocalizedCopy Intermission = new("KISA MOLA", "INTERMISSION");
        public static readonly LocalizedCopy Paused = new("DURAKLATILDI", "PAUSED");
        public static readonly LocalizedCopy Victory = new("ZAFER", "VICTORY");
        public static readonly LocalizedCopy Defeat = new("YENİLGİ", "DEFEAT");
        public static readonly LocalizedCopy Loading = new("YÜKLENİYOR", "LOADING");
        public static readonly LocalizedCopy PhaseBossApproaching = new("BOSS YAKLAŞIYOR", "BOSS APPROACHING");
        public static readonly LocalizedCopy SelectSite = new("{0} · Yer seç", "{0} · Select site");
        public static readonly LocalizedCopy GoldNeed = new("{0} altın · {1} eksik", "{0} gold · Need {1}");
        public static readonly LocalizedCopy NotEnough = new("YETERSİZ", "NOT ENOUGH");
        public static readonly LocalizedCopy Ready = new("HAZIR", "READY");
        public static readonly LocalizedCopy Returning = new("DÖNÜYOR", "RETURNING");
        public static readonly LocalizedCopy AbilityPaused = new("DURAKLADI", "PAUSED");
        public static readonly LocalizedCopy AttackRate = new("{0:0.0}/sn", "{0:0.0}/sec");
        public static readonly LocalizedCopy GoldCost = new("{0} altın", "{0} gold");
        public static readonly LocalizedCopy MaxLevelLabel = new("Maks", "Max");
        public static readonly LocalizedCopy TemporarilyDisruptedSellForGold = new("Geçici baskı altında • Satış {0} altın", "Temporarily disrupted • Sell for {0} gold");
        public static readonly LocalizedCopy UpgradeForGoldSellForGold = new("Yükseltme {0} altın • Satış {1} altın", "Upgrade for {0} gold • Sell for {1} gold");
        public static readonly LocalizedCopy MaxLevelSellForGold = new("Maks seviye • Satış {0} altın", "Max level • Sell for {0} gold");
        public static readonly LocalizedCopy Damage = new("HASAR", "DAMAGE");
        public static readonly LocalizedCopy Range = new("MENZİL", "RANGE");
        public static readonly LocalizedCopy Rate = new("HIZ", "RATE");
        public static readonly LocalizedCopy UpgradeStat = new("YÜKSELTME", "UPGRADE");
        public static readonly LocalizedCopy SellStat = new("SATIŞ", "SELL");
        public static readonly LocalizedCopy MaxLv = new("MAKS\nSV", "MAX\nLV");
        public static readonly LocalizedCopy OutpostSlot = new("KARAKOL YUVASI", "OUTPOST SLOT");
        public static readonly LocalizedCopy ChooseADefenceType = new("Savunma türünü seç", "Choose a defence type");
        public static readonly LocalizedCopy BuildArcherBarracksOrMage = new("Okçu, Kışla veya Büyü kur.", "Build Archer, Barracks, or Mage.");
        public static readonly LocalizedCopy UpgradeLocked = new("YÜKSELT\nKAPALI", "UPGRADE\nLOCKED");
        public static readonly LocalizedCopy SellLocked = new("SAT\nKAPALI", "SELL\nLOCKED");
        public static readonly LocalizedCopy PauseHint = new("Savaşa devam edebilir veya mevcut kuşatmadan ayrılıp ana menüye dönebilirsin.", "Resume the battle or return to the main menu.");
        public static readonly LocalizedCopy Resume = new("DEVAM ET", "RESUME");
        public static readonly LocalizedCopy MainMenu = new("ANA MENÜ", "MAIN MENU");
        public static readonly LocalizedCopy NoSelection = new("SEÇİM YOK", "NO SELECTION");
        public static readonly LocalizedCopy SelectionHint = new("Bir yuva ya da karakol seçildiğinde ayrıntılar burada görünür.", "Select a slot or outpost to see its details.");
        public static readonly LocalizedCopy ImportGuestProgress = new("MİSAFİR İLERLEMESİNİ AKTAR", "IMPORT GUEST PROGRESS");
        public static readonly LocalizedCopy GuestImportExplanation = new("Bu cihazın misafir bölüm başarıları açık hesaba aktarılacak. Misafir kaydı saklanır; hesap değiştirmek otomatik aktarım yapmaz.", "This device's guest level results will be imported into the current account. The guest save stays on this device; switching accounts never imports automatically.");
        public static readonly LocalizedCopy Import = new("AKTAR", "IMPORT");
        public static readonly LocalizedCopy PlaceAMageAgainstArmouredTargets = new("Zırhlı hedefe büyücü yerleştir.", "Place a mage against armoured targets.");
        public static readonly LocalizedCopy SharpshootersTargetHealersFirst = new("Keskin Nişancı şifacıları önce hedefler.", "Sharpshooters target healers first.");
        public static readonly LocalizedCopy UseStunningStrikeDuringTheRedWarning = new("Kırmızı uyarı sırasında menzildeyse Sarsıcı Darbe kullan.", "Use Stunning Strike during the red warning when the attacker is in range.");
        public static readonly LocalizedCopy FocusTwoTowersOnTheSameTarget = new("Aynı hedefe iki kuleyle yoğunlaş.", "Focus two towers on the same target.");
        public static readonly LocalizedCopy AddOrUpgradeAnArcherAtThe = new("Bir sonraki yol kıvrımına okçu ekle veya yükselt.", "Add or upgrade an archer at the next bend.");
        public static readonly LocalizedCopy HeroNameInvalid = new("En fazla 20 karakter kullan; satır sonu ve < > ekleme.", "Use up to 20 characters, without line breaks or < >.");
        public static readonly LocalizedCopy HeroNameSaveFailed = new("Ad kaydedilemedi. Yeniden dene.", "Name could not be saved. Please try again.");
        public static readonly LocalizedCopy Saved = new("Kaydedildi: {0}", "Saved: {0}");
        public static readonly LocalizedCopy Continue = new("DEVAM ET  ›", "CONTINUE  ›");
        public static readonly LocalizedCopy PlayNow = new("OYUNA BAŞLA  ›", "PLAY NOW  ›");
        public static readonly LocalizedCopy GuestDefender = new("Misafir Muhafız", "Guest Defender");
        public static readonly LocalizedCopy OpeningTheSecureAccountWindow = new("Güvenli hesap penceresi açılıyor...", "Opening the secure account window...");
        public static readonly LocalizedCopy AccountSignInUnavailable = new("Çevrimiçi giriş bu geliştirme sürümünde henüz etkin değil. Misafir olarak devam edebilirsin.", "Online sign-in is not enabled in this development build yet. You can continue as a guest.");
        public static readonly LocalizedCopy Enemies = new("DÜŞMANLAR", "ENEMIES");
        public static readonly LocalizedCopy Objective = new("HEDEF", "OBJECTIVE");
        public static readonly LocalizedCopy Locked = new("KİLİTLİ", "LOCKED");
        public static readonly LocalizedCopy MapLevelStars = new("{0}/3 YILDIZ", "{0}/3 STARS");
        public static readonly LocalizedCopy MapReady = new("AÇIK", "READY");
        public static readonly LocalizedCopy LinkedAccount = new("Bağlı hesap", "Linked account");
        public static readonly LocalizedCopy Guest = new("Misafir", "Guest");
        public static readonly LocalizedCopy GreenwatchDefender = new("Greenwatch Muhafızı", "Greenwatch Defender");
        public static readonly LocalizedCopy ProfileStars = new("{0}/30 yıldız", "{0}/30 stars");
        public static readonly LocalizedCopy MapNoStars = new("0/3 YILDIZ", "0/3 STARS");
        public static readonly LocalizedCopy PrepareYourFirstDefence = new("İLK SAVUNMANA HAZIRLAN  ›", "PREPARE YOUR FIRST DEFENCE  ›");
        public static readonly LocalizedCopy VictoryComplete = new("ZAFER\n{0} TAMAMLANDI", "VICTORY\n{0} COMPLETE");
        public static readonly LocalizedCopy TheDefenceLineHasFallen = new("SAVUNMA HATTI DÜŞTÜ", "THE DEFENCE LINE HAS FALLEN");
        public static readonly LocalizedCopy ResultStars = new("{0} / 3 YILDIZ", "{0} / 3 STARS");
        public static readonly LocalizedCopy ResultNoStars = new("0 / 3 YILDIZ", "0 / 3 STARS");
        public static readonly LocalizedCopy ResultGuestAccountHint = new("İlerlemeni korumak için menüden hesabını bağlayabilirsin.", "You can link an account from the menu to protect your progress.");
        public static readonly LocalizedCopy Wave = new("Dalga {0} · ", "Wave {0} · ");
        public static readonly LocalizedCopy BattlePreparationFailedPleaseTryAgain = new("Savaş hazırlanamadı. Yeniden deneyebilirsin.", "Battle preparation failed. Please try again.");
        public static readonly LocalizedCopy OrientationFailed = new("Ekran yönü değiştirilemedi.\nCihazı çevirip yeniden deneyebilirsin.", "The screen could not rotate.\nTurn your device and try again.");
        public static readonly LocalizedCopy PreparingTheBattlefieldRotatingToLandscape = new("SAVAŞ ALANI HAZIRLANIYOR\nCİHAZ YATAYA ALINIYOR", "PREPARING THE BATTLEFIELD\nROTATING TO LANDSCAPE");
        public static readonly LocalizedCopy PreparingTheRealmMapRotatingToPortrait = new("KRALLIK HARİTASI HAZIRLANIYOR\nCİHAZ PORTREYE ALINIYOR", "PREPARING THE REALM MAP\nROTATING TO PORTRAIT");
        public static readonly LocalizedCopy LevelLoadFailed = new("Bölüm yüklenemedi. Yeniden deneyebilirsin.", "The level could not load. Please try again.");
        public static readonly LocalizedCopy PreparingTheBattlefield = new("Savaş alanı hazırlanıyor…", "Preparing the battlefield…");
        public static readonly LocalizedCopy TowerDamageStats = new("HASAR {0} · {1}\nMENZİL {2:0.0}\nATIŞ {3:0.0}/s", "DAMAGE {0} · {1}\nRANGE {2:0.0}\nRATE {3:0.0}/s");
        public static readonly LocalizedCopy MagicDamage = new("Büyü", "Magic");
        public static readonly LocalizedCopy PhysicalDamage = new("Fiziksel", "Physical");
        public static readonly LocalizedCopy HudGold = new("ALTIN", "GOLD");
        public static readonly LocalizedCopy HudKeep = new("KALE", "KEEP");
        public static readonly LocalizedCopy HudWave = new("DALGA", "WAVE");
        public static readonly LocalizedCopy HudPreparing = new("HAZIRLIK", "PREPARING");
        public static readonly LocalizedCopy HudCombat = new("SAVAŞ", "COMBAT");
        public static readonly LocalizedCopy HudIntermission = new("MOLA", "INTERMISSION");
        public static readonly LocalizedCopy HudPaused = new("DURAKLATILDI", "PAUSED");
        public static readonly LocalizedCopy HudLoading = new("YÜKLENİYOR", "LOADING");
        public static readonly LocalizedCopy HudPause = new("DURAKLAT", "PAUSE");
        public static readonly LocalizedCopy HudContinue = new("DEVAM", "RESUME");
        public static readonly LocalizedCopy HudArcher = new("OKÇU", "ARCHER");
        public static readonly LocalizedCopy HudBarracks = new("KIŞLA", "BARRACKS");
        public static readonly LocalizedCopy HudMage = new("BÜYÜ", "MAGE");
        public static readonly LocalizedCopy HudShieldBash = new("SARSICI DARBE", "STUNNING STRIKE");
        public static readonly LocalizedCopy HudCancel = new("İPTAL", "CANCEL");
        public static readonly LocalizedCopy HudFirstWave = new("İLK DALGAYI BAŞLAT", "START FIRST WAVE");
        public static readonly LocalizedCopy HudEarlyWave = new("DALGAYI ERKEN ÇAĞIR", "CALL WAVE EARLY");
        public static readonly LocalizedCopy HudNextWave = new("SONRAKİ DALGA", "NEXT WAVE");
        public static readonly LocalizedCopy HudUpgrade = new("YÜKSELT", "UPGRADE");
        public static readonly LocalizedCopy HudSell = new("SAT", "SELL");
        public static readonly LocalizedCopy HudKnight = new("Savaşçı", "Warrior");
        public static readonly LocalizedCopy HudAbilityReady = new("YETENEK HAZIR", "READY");
        public static readonly LocalizedCopy HudKnightReturning = new("Savaşçı dönüyor", "Warrior returns");
        public static readonly LocalizedCopy HudVictory = new("ZAFER\nGREENWATCH KORUNDU", "VICTORY\nGREENWATCH DEFENDED");
        public static readonly LocalizedCopy HudDefeat = new("YENİLGİ\nGREENWATCH DÜŞTÜ", "DEFEAT\nGREENWATCH HAS FALLEN");
        public static readonly LocalizedCopy HudVictoryPhase = new("ZAFER", "VICTORY");
        public static readonly LocalizedCopy HudDefeatPhase = new("YENİLGİ", "DEFEAT");
        public static readonly LocalizedCopy HudRestart = new("YENİDEN DENE", "TRY AGAIN");
        public static readonly LocalizedCopy HudSlotInstruction = new("KARAKOL PLATFORMU", "Choose an outpost below.");
        public static readonly LocalizedCopy HudBuildInstruction = new("Aşağıdan bir karakol seç.", "Tap an outpost platform and build your defence.");
        public static readonly LocalizedCopy SingleEntrance = new("{0} giriş", "{0} entrance");
        public static readonly LocalizedCopy MultipleEntrances = new("{0} giriş", "{0} entrances");
        public static readonly LocalizedCopy MenuStart = new("BAŞLAT", "START");
        public static readonly LocalizedCopy MenuMap = new("HARİTA", "MAP");
        public static readonly LocalizedCopy MenuHero = new("KAHRAMAN", "HERO");
        public static readonly LocalizedCopy MenuChapter = new("Bölge {0} · {1} · Bölüm {2}", "Region {0} · {1} · Chapter {2}");
        public static readonly LocalizedCopy MenuWaves = new("{0} dalga", "{0} waves");
        public static readonly LocalizedCopy MenuMinutes = new("~{0:0.#} dk", "~{0:0.#} min");
        public static readonly LocalizedCopy MenuRegionProgress = new("BÖLGE İLERLEMESİ  {0} / 10", "REGION PROGRESS  {0} / 10");
        public static readonly LocalizedCopy ChapterDescription = new("Yolu tut, kaleyi koru. Şafağa kadar dayan.", "Hold the road. Protect the keep until dawn.");
        public static readonly LocalizedCopy BriefingDetails = new("{0} dalga · ~{1:0.#} dk · {2} altın · {3}", "{0} waves · ~{1:0.#} min · {2} gold · {3}");
        public static readonly LocalizedCopy ContinueDetails = new("BÖLGE {0}  ·  {1}  ·  {2}\n{3} dalga  ·  ~{4:0.#} dk  ·  {5} başlangıç altını", "REGION {0}  ·  {1}  ·  {2}\n{3} waves  ·  ~{4:0.#} min  ·  {5} starting gold");
        public static readonly LocalizedCopy ThreatLow = new("I  Düşük", "I  Low");
        public static readonly LocalizedCopy ThreatRising = new("II  Yükseliyor", "II  Rising");
        public static readonly LocalizedCopy ThreatHigh = new("III  Yüksek", "III  High");
        public static readonly LocalizedCopy ThreatLord = new("LORD SALDIRISI", "LORD ASSAULT");
        public static readonly LocalizedCopy Done = new("TAMAM", "DONE");
    }
}
