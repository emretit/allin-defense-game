namespace AllinDefense.UI
{
    /// <summary>A bilingual label bound explicitly, independent of its Turkish wording.</summary>
    public readonly struct LocalizedCopy
    {
        public string Turkish { get; }
        public string English { get; }
        public bool IsLocalized { get; }
        public string Value => GameLocalization.Text(Turkish, English);

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
        public static readonly LocalizedCopy ObjectiveSiege = new("Kuşatma saldırısını darbeyle kes.", "Interrupt siege attacks with your strike.");
        public static readonly LocalizedCopy ObjectiveMixedPressure = new("Karma düşman baskısına hazırlan.", "Prepare for mixed enemy pressure.");
        public static readonly LocalizedCopy ObjectiveLord = new("Lordun kırmızı uyarısını darbeyle kes.", "Interrupt the lord's red warning.");
        public static readonly LocalizedCopy SaveRetry = new("Kayıt tekrar denenecek", "Save will retry");
        public static readonly LocalizedCopy SaveRecovery = new("Kayıt kurtarılmayı bekliyor", "Save needs recovery");
        public static readonly LocalizedCopy TransferReview = new("Aktarım kontrolü gerekiyor", "Transfer needs review");
        public static readonly LocalizedCopy WaitingForConnection = new("Bağlantı bekleniyor", "Waiting for connection");
        public static readonly LocalizedCopy Done = new("TAMAM", "DONE");
    }
}
